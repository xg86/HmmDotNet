﻿using HmmDotNet.MachineLearning.Algorithms.VaribaleEstimationCalculator.Base;
using HmmDotNet.MachineLearning.Algorithms.VaribaleEstimationCalculator.EstimationParameters;
using HmmDotNet.Mathematic.Extentions;

namespace HmmDotNet.MachineLearning.Algorithms.VaribaleEstimationCalculator
{
    public class PiEstimator : IVariableEstimator<double[], PiParameters>
    {
        private double[] _estimatedPi;

        public double[] Estimate(PiParameters parameters)
        {
            if (_estimatedPi != null)
            {
                return _estimatedPi;
            }
            _estimatedPi = new double[parameters.N];

            for (var i = 0; i < parameters.N; i++)
            {
                _estimatedPi[i] = (parameters.Normalized) ? LogExtention.eExp(parameters.Gamma[0][i]) : parameters.Gamma[0][i];
            }

            return _estimatedPi;
        }
    }
}
