using System;
using System.ComponentModel.DataAnnotations;

namespace Singluar.Utilities.Attributes
{
    /// <summary>
    /// Checks if the given decimal attribute is greater than zero.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public class IsPositive : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value is decimal)
            {
                return (decimal)value > 0;
            }

            return false;
        }
    }
}
