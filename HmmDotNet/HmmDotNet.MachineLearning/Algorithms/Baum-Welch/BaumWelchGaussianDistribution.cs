﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using HmmDotNet.MachineLearning.Base;
using HmmDotNet.MachineLearning.Estimators;
using HmmDotNet.MachineLearning.HiddenMarkovModels;
using HmmDotNet.Mathematic.Extentions;
using HmmDotNet.Statistics.Distributions.Univariate;

namespace HmmDotNet.MachineLearning.Algorithms
{
    // TODO : Calculate alpha[t][j].AlphaValue * beta[t][j].BetaValue once
    public class BaumWelchGaussianDistribution : BaseBaumWelch<NormalDistribution>, IBaumWelchAlgorithm<NormalDistribution>
    {
        #region Private Members

        private GammaEstimator<NormalDistribution> _gammaEstimator;
        private KsiEstimator<NormalDistribution> _ksiEstimator;
        private MuEstimator<NormalDistribution> _muEstimator;
        private SigmaEstimator<NormalDistribution> _sigmaEstimator; 
 
        private IHiddenMarkovModel<NormalDistribution> _estimatedModel;
        private IHiddenMarkovModel<NormalDistribution> _currentModel; 
       
        private readonly IList<IObservation> _observations;

        private readonly NormalDistribution[] _estimatedEmissions;

        #endregion Private Members

        #region Constructors

        public BaumWelchGaussianDistribution(IList<IObservation> observations, IHiddenMarkovModel<NormalDistribution> model): base(model)
        {
            _currentModel = model;
            _observations = observations;

            _estimatedEmissions = new NormalDistribution[model.N];
            for (var i = 0; i < model.N; i++)
            {
                _estimatedEmissions[i] = new NormalDistribution();
            }

            _estimatedModel = HiddenMarkovModelStateFactory.GetState(new ModelCreationParameters<NormalDistribution> { Pi = _estimatedPi, TransitionProbabilityMatrix = _estimatedTransitionProbabilityMatrix, Emissions = _estimatedEmissions });//new HiddenMarkovModelState<NormalDistribution>(_estimatedPi, _estimatedTransitionProbabilityMatrix, _estimatedEmissions);
            Normalized = model.Normalized;
        }

        #endregion Constructors

        #region IBaumWelchAlgorithm Members

        public bool Normalized { get; set; }

        public IHiddenMarkovModel<NormalDistribution> Run(int maxIterations, double likelihoodTolerance)
        {
            // Initialize responce object
            var forwardBackward = new ForwardBackward(Normalized);
            
            do
            {
                maxIterations--;
                if (!_estimatedModel.Likelihood.EqualsTo(0))
                {
                    _currentModel = HiddenMarkovModelStateFactory.GetState(new ModelCreationParameters<NormalDistribution> { Pi = _estimatedPi, TransitionProbabilityMatrix = _estimatedTransitionProbabilityMatrix, Emissions = _estimatedEmissions });//new HiddenMarkovModelState<NormalDistribution>(_estimatedPi, _estimatedTransitionProbabilityMatrix, _estimatedEmissions);
                    _currentModel.Likelihood = _estimatedModel.Likelihood;
                }
                // Run Forward-Backward procedure
                forwardBackward.RunForward(_observations, _currentModel);
                forwardBackward.RunBackward(_observations, _currentModel);

                var parameters = new ParameterEstimations<NormalDistribution>(_currentModel, _observations, forwardBackward.Alpha, forwardBackward.Beta);
                _gammaEstimator = new GammaEstimator<NormalDistribution>(parameters, Normalized);
                _ksiEstimator = new KsiEstimator<NormalDistribution>(parameters, Normalized);
                _muEstimator = new MuEstimator<NormalDistribution>(_currentModel, _observations);
                _sigmaEstimator = new SigmaEstimator<NormalDistribution>(_currentModel, _observations);

                EstimatePi(_gammaEstimator.Gamma);
                EstimateTransitionProbabilityMatrix(_gammaEstimator.Gamma, _ksiEstimator.Ksi, _observations.Count);
                // Estimate observation probabilities
                var muVector = _muEstimator.MuUnivariate(_gammaEstimator.Gamma);
                var sigmaVector = _sigmaEstimator.SigmaUnivariate(_gammaEstimator.Gamma, muVector);
                for (var j = 0; j < _currentModel.N; j++)
                {
                    _estimatedEmissions[j] = new NormalDistribution(muVector[j], sigmaVector[j]);
                }
                _estimatedModel = HiddenMarkovModelStateFactory.GetState(new ModelCreationParameters<NormalDistribution> { Pi = _estimatedPi, TransitionProbabilityMatrix = _estimatedTransitionProbabilityMatrix, Emissions = _estimatedEmissions });//new HiddenMarkovModelState<NormalDistribution>(_estimatedPi, _estimatedTransitionProbabilityMatrix, _estimatedEmissions) { LogNormalized = _currentModel.LogNormalized };
                _estimatedModel.Likelihood = forwardBackward.RunForward(_observations, _estimatedModel);
                _likelihoodDelta = Math.Abs(Math.Abs(_currentModel.Likelihood) - Math.Abs(_estimatedModel.Likelihood));
                Debug.WriteLine("Iteration {3} , Current {0}, Estimate {1} Likelihood delta {2}", _currentModel.Likelihood, _estimatedModel.Likelihood, _likelihoodDelta, maxIterations);
            }
            while (!_currentModel.Equals(_estimatedModel) && maxIterations > 0 && _likelihoodDelta > likelihoodTolerance);

            return _estimatedModel;
        }
      
        #endregion
    }
}