using System;
using System.Text.RegularExpressions;
using QuickBrain.Modules;

namespace Community.PowerToys.Run.Plugin.QuickBrain
{
    /// <summary>
    /// Builds user-friendly error messages with context and examples.
    /// Transforms technical errors into actionable guidance.
    /// </summary>
    public static class ErrorMessageBuilder
    {
        /// <summary>
        /// Build a contextual error result from an exception.
        /// </summary>
        /// <param name="ex">The exception that occurred</param>
        /// <param name="input">The user's input query</param>
        /// <returns>A detailed CalculationResult with helpful error message</returns>
        public static CalculationResult BuildError(Exception ex, string input)
        {
            return ex switch
            {
                DivideByZeroException => new CalculationResult
                {
                    Title = "Error: Division by Zero",
                    SubTitle = "Cannot divide by zero. Check your expression for '÷0' or '/0'.",
                    Result = "Invalid",
                    ErrorMessage = "Division by zero is mathematically undefined",
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                OverflowException => new CalculationResult
                {
                    Title = "Error: Number Overflow",
                    SubTitle = $"Result too large to calculate. Try smaller numbers. ({ex.Message})",
                    Result = "Overflow",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                FormatException when ContainsConversionPattern(input) => new CalculationResult
                {
                    Title = "Error: Invalid Unit Conversion",
                    SubTitle = "Cannot parse units. Example: '100 km to miles' or '70 kg to pounds'",
                    Result = "Invalid Format",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                FormatException when ContainsDatePattern(input) => new CalculationResult
                {
                    Title = "Error: Invalid Date Format",
                    SubTitle = "Use format: YYYY-MM-DD. Example: '2025-01-15' or 'today + 7 days'",
                    Result = "Invalid Date",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                ArgumentException when input.Contains("(") && input.Contains(")") => new CalculationResult
                {
                    Title = "Error: Invalid Function Syntax",
                    SubTitle = $"{ex.Message}. Examples: 'sin(45)', 'sqrt(25)', 'log(100)'",
                    Result = "Syntax Error",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                ArgumentException when input.Contains("(") && !input.Contains(")") => new CalculationResult
                {
                    Title = "Error: Mismatched Parentheses",
                    SubTitle = "Missing closing parenthesis ')'. Check your expression.",
                    Result = "Syntax Error",
                    ErrorMessage = "Unmatched parentheses",
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                ArgumentException => new CalculationResult
                {
                    Title = "Error: Invalid Syntax",
                    SubTitle = $"{ex.Message}. Try: '2+2', '100 km to miles', or 'sin(pi/4)'",
                    Result = "Invalid Expression",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                NotSupportedException => new CalculationResult
                {
                    Title = "Error: Operation Not Supported",
                    SubTitle = $"{ex.Message}. See supported operations in settings.",
                    Result = "Not Supported",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                TimeoutException or OperationCanceledException => new CalculationResult
                {
                    Title = "Error: Operation Timeout",
                    SubTitle = "Calculation took too long. Try a simpler expression or increase timeout in settings.",
                    Result = "Timeout",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                },

                _ => new CalculationResult
                {
                    Title = "Error: Cannot Process Query",
                    SubTitle = GetGenericErrorMessage(input, ex),
                    Result = "Error",
                    ErrorMessage = ex.Message,
                    Type = CalculationType.Error,
                    IsError = true,
                    RawExpression = input,
                    Score = 0
                }
            };
        }

        /// <summary>
        /// Build a simple error with custom message.
        /// </summary>
        public static CalculationResult BuildSimpleError(string title, string message, string? input = null)
        {
            return new CalculationResult
            {
                Title = title,
                SubTitle = message,
                Result = "Error",
                ErrorMessage = message,
                Type = CalculationType.Error,
                IsError = true,
                RawExpression = input,
                Score = 0
            };
        }

        private static bool ContainsConversionPattern(string input)
        {
            return input.Contains("to ", StringComparison.OrdinalIgnoreCase) ||
                   input.Contains("in ", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(input, @"\d+\s*(km|kg|lb|celsius|fahrenheit|miles|meters|pounds)", RegexOptions.IgnoreCase);
        }

        private static bool ContainsDatePattern(string input)
        {
            return Regex.IsMatch(input, @"\d{4}-\d{2}-\d{2}") ||
                   input.Contains("today", StringComparison.OrdinalIgnoreCase) ||
                   input.Contains("tomorrow", StringComparison.OrdinalIgnoreCase) ||
                   input.Contains("yesterday", StringComparison.OrdinalIgnoreCase) ||
                   Regex.IsMatch(input, @"(days?|weeks?|months?|years?)", RegexOptions.IgnoreCase);
        }

        private static string GetGenericErrorMessage(string input, Exception ex)
        {
            // Try to give context-specific hints
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Empty expression. Try: '2+2', '100 km to miles', or 'ai explain recursion'";
            }

            if (input.Length < 2)
            {
                return "Expression too short. Examples: '2+2', 'pi', or 'help'";
            }

            if (Regex.IsMatch(input, @"^[a-zA-Z]+$"))
            {
                return $"Unknown command '{input}'. Try: 'ai {input}' for AI help, or see examples in settings.";
            }

            // Generic fallback with exception info
            var message = $"Cannot process '{input}'.";
            if (!string.IsNullOrWhiteSpace(ex.Message) && ex.Message.Length < 100)
            {
                message += $" Reason: {ex.Message}";
            }
            message += " Try: '2+2', 'sin(45)', or '100 km to miles'";

            return message;
        }
    }
}
