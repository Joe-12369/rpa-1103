using II.RPA.WinAutomation.CommonUtility;
using static II.RPA.WinAutomation.CommonUtility.I18nExtensions;
using System;
using System.Runtime.CompilerServices;

namespace II.RPA.Plugin.GUI.Commons.MouseMotions.Algorithm1
{
    internal class Flow
    {
        private static readonly int AVERAGE_BUCKET_VALUE = 100;
        private readonly double[] _buckets;

        public Flow(double[] characteristics) =>
            _buckets = NormalizeBuckets(characteristics);

        private double[] NormalizeBuckets(double[] flowCharacteristics)
        {
            double[] numArray = new double[flowCharacteristics.Length];
            double num1 = 0.0;

            for (int index = 0; index < flowCharacteristics.Length; ++index)
            {
                if (flowCharacteristics[index] < 0.0)
                {
                    throw new InvalidOperationException(Localize($"速度特征不能小于0", "controlType_Flow_5A6D2639"));
                }

                num1 += flowCharacteristics[index];
            }
            if (num1 == 0.0)
            {
                throw new Exception("速度特征求和不能等于0".I18n("controlType_Flow_4414A423"));
            }

            double num2 = AVERAGE_BUCKET_VALUE * (double)numArray.Length / num1;

            for (int index = 0; index < flowCharacteristics.Length; ++index)
            {
                numArray[index] = flowCharacteristics[index] * num2;
            }

            return numArray;
        }

        public double[] GetFlowCharacteristics() => _buckets;

        public double GetStepSize(double distance, int steps, double completion)
        {
            double num = 1.0 / steps;
            return GetBucketsContents(
                completion * _buckets.Length,
                (completion + num) * _buckets.Length) * (distance / (_buckets.Length * AVERAGE_BUCKET_VALUE));
        }

        private double GetBucketsContents(double bucketFrom, double bucketUntil)
        {
            double bucketsContents = 0.0;

            for (int index = (int)bucketFrom; index < bucketUntil; ++index)
            {
                double bucket = _buckets[index];
                double num1 = 1.0;
                double num2 = 0.0;

                if (bucketUntil < index + 1)
                {
                    num1 = bucketUntil - (int)bucketUntil;
                }

                if ((int)bucketFrom == index)
                {
                    num2 = bucketFrom - (int)bucketFrom;
                }

                double num3 = bucket * (num1 - num2);
                bucketsContents += num3;
            }

            return bucketsContents;
        }
    }
}
