using System;

namespace Contract
{
    // Plugin result types ARE required to be serializable
    [Serializable]
    public class CalculationResult
    {
        public decimal Result { get; set; }
    }
}