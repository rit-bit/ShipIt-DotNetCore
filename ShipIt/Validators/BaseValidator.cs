using System.Collections.Generic;

 namespace ShipIt.Validators
{
    public abstract class BaseValidator<T>
    {
        private readonly List<string> _errors;

        protected BaseValidator()
        {
            _errors = new List<string>();
        }

        public void Validate(T target)
        {
            DoValidation(target);
        }

        protected abstract void DoValidation(T target);

        private void AddError(string error)
        {
            _errors.Add(error);
        }

        private void AddErrors(IEnumerable<string> errorsToAdd)
        {
            _errors.AddRange(errorsToAdd);
        }

/**
 * Object validators
 */
        private void AssertNotNull(string fieldName, object value)
        {
            if (value == null)
            {
                AddError($"Field {fieldName} cannot be null");
            }
        }

/**
 * String validators
 */

        protected void AssertNotBlank(string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                AddError($"Field {fieldName} cannot be blank");
            }
        }

        protected void AssertNumeric(string fieldName, string value)
        {
            double d;
            if (!double.TryParse(value, out d))
            {
                AddError($"Field {fieldName} must be numeric");
            }
        }

        protected void AssertMaxLength(string fieldName, string value, int maxLength)
        {
            if (value.Length > maxLength)
            {
                AddError($"Field {fieldName} must be shorter than {maxLength} characters");
            }
        }

        protected void AssertExactLength(string fieldName, string value, int exactLength)
        {
            if (value.Length != exactLength)
            {
                AddError($"Field {fieldName} must be exactly {exactLength} characters");
            }
        }

/**
 * Numeric validators
 */

        protected void AssertNonNegative(string fieldName, int value)
        {
            if (value < 0)
            {
                AddError($"Field {fieldName} must be non-negative");
            }
        }

        protected void AssertNonNegative(string fieldName, float value)
        {
            if (value < 0)
            {
                AddError($"Field {fieldName} must be non-negative");
            }
        }

/**
 * Specific validators
 */

        protected void ValidateGtin(string value)
        {
            AssertNotBlank("gtin", value);
            AssertNumeric("gtin", value);
            AssertMaxLength("gtin", value, 13);
        }

        protected void ValidateGcp(string value)
        {
            AssertNotBlank("gcp", value);
            AssertNumeric("gcp", value);
            AssertMaxLength("gcp", value, 13);
        }

        protected void ValidateWarehouseId(int warehouseId)
        {
            AssertNonNegative("warehouseId", warehouseId);
        }
    }
}