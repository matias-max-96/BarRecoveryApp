using System.Globalization;
using BarRecoveryApp.Models.Catalogs;
using BarRecoveryApp.Models.Enums;

namespace BarRecoveryApp.ViewModels.Items
{
    public class QualityInspectionAttributeItemViewModel : BaseViewModel
    {
        private string _valueText = string.Empty;
        private bool _wasMeasured = true;
        private bool? _valueBool;

        public BarAttributeDefinition Definition { get; set; } = default!;

        public string AttributeDefinitionId => Definition.Id;

        public string Code => Definition.Code;

        public string Name => Definition.Name;

        public AttributeDataType DataType => Definition.DataType;

        public string Unit => Definition.Unit ?? string.Empty;

        public bool IsRequired => Definition.IsRequired;

        public bool HasRangeValidation => Definition.HasRangeValidation;

        public double? MinValue => Definition.MinValue;

        public double? MaxValue => Definition.MaxValue;

        public string ToleranceText => Definition.ToleranceText ?? string.Empty;

        public bool IsBoolean => DataType == AttributeDataType.Boolean;

        public bool IsNumeric =>
            DataType == AttributeDataType.Decimal ||
            DataType == AttributeDataType.Integer;

        public bool ShowNumericInput
        {
            get
            {
                return WasMeasured && IsNumeric;
            }
        }

        public bool ShowBooleanInput
        {
            get
            {
                return WasMeasured && IsBoolean;
            }
        }

        public bool IsText => DataType == AttributeDataType.Text;

        public bool IsDate => DataType == AttributeDataType.Date;

        public string ValueText
        {
            get => _valueText;
            set
            {
                if (SetProperty(ref _valueText, value))
                {
                    OnPropertyChanged(nameof(IsOutOfRange));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(HasValue));
                }
            }
        }

        public bool WasMeasured
        {
            get => _wasMeasured;
            set
            {
                if (SetProperty(ref _wasMeasured, value))
                {
                    if (!value)
                    {
                        ValueText = string.Empty;
                        ValueBool = null;
                    }

                    OnPropertyChanged(nameof(ShowNumericInput));
                    OnPropertyChanged(nameof(ShowBooleanInput));
                    OnPropertyChanged(nameof(IsOutOfRange));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(HasValue));
                }
            }
        }

        public bool? ValueBool
        {
            get => _valueBool;
            set
            {
                if (SetProperty(ref _valueBool, value))
                {
                    OnPropertyChanged(nameof(BooleanYes));
                    OnPropertyChanged(nameof(BooleanNo));
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(HasValue));
                }
            }
        }

        public bool HasValue
        {
            get
            {
                if (!WasMeasured)
                    return false;

                if (IsBoolean)
                    return ValueBool.HasValue;

                return !string.IsNullOrWhiteSpace(ValueText);
            }
        }

        public bool IsOutOfRange
        {
            get
            {
                if (!WasMeasured)
                    return false;

                if (!HasRangeValidation)
                    return false;

                if (!IsNumeric)
                    return false;

                if (!TryGetNumericValue(out var numericValue))
                    return false;

                if (MinValue.HasValue && numericValue < MinValue.Value)
                    return true;

                if (MaxValue.HasValue && numericValue > MaxValue.Value)
                    return true;

                return false;
            }
        }

        public string StatusText
        {
            get
            {
                if (!WasMeasured)
                    return "No medido";

                if (IsBoolean)
                {
                    if (!ValueBool.HasValue)
                        return "Resultado pendiente";

                    return ValueBool.Value
                        ? "Resultado: Sí"
                        : "Resultado: No";
                }

                if (HasRangeValidation && IsNumeric)
                {
                    if (string.IsNullOrWhiteSpace(ValueText))
                    {
                        return string.IsNullOrWhiteSpace(ToleranceText)
                            ? "Ingrese valor medido"
                            : $"Rango esperado: {ToleranceText}";
                    }

                    return IsOutOfRange
                        ? $"Fuera de rango. Esperado: {ToleranceText}"
                        : $"Dentro de rango. Esperado: {ToleranceText}";
                }

                if (!string.IsNullOrWhiteSpace(Unit))
                    return $"Unidad: {Unit}";

                return string.Empty;
            }
        }

        public string StatusColor
        {
            get
            {
                if (!WasMeasured)
                    return "Gray";

                if (IsOutOfRange)
                    return "#B00020";

                if (HasRangeValidation &&
                    IsNumeric &&
                    !string.IsNullOrWhiteSpace(ValueText))
                {
                    return "#2E7D32";
                }

                return "Gray";
            }
        }
        public bool BooleanYes
        {
            get => ValueBool == true;
            set
            {
                if (value)
                {
                    ValueBool = true;
                }
                else if (ValueBool == true)
                {
                    ValueBool = null;
                }
            }
        }

        public bool BooleanNo
        {
            get => ValueBool == false;
            set
            {
                if (value)
                {
                    ValueBool = false;
                }
                else if (ValueBool == false)
                {
                    ValueBool = null;
                }
            }
        }
        public bool TryGetNumericValue(out double value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(ValueText))
                return false;

            return double.TryParse(
                ValueText.Trim().Replace(",", "."),
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out value);
        }

        public double? GetNullableNumericValue()
        {
            return TryGetNumericValue(out var value)
                ? value
                : null;
        }

    }
}