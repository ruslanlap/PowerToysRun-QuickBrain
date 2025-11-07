using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickBrain.Modules;

public class ExpressionParser
{
    private readonly Settings _settings;
    private readonly MathEngine _mathEngine;
    private readonly Converter _converter;
    private readonly DateCalc _dateCalc;
    private readonly LogicEval _logicEval;

    public ExpressionParser(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _mathEngine = new MathEngine(settings);
        _converter = new Converter(settings);
        _dateCalc = new DateCalc(settings);
        _logicEval = new LogicEval(settings);
    }

    public ParsedExpression? Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ParsedExpression
            {
                OriginalInput = input,
                NormalizedInput = string.Empty,
                Type = ExpressionType.Unknown,
                IsValid = false,
                ErrorMessage = "Input cannot be empty"
            };

        try
        {
            var normalizedInput = NormalizeInput(input);
            var expressionType = DetermineExpressionType(normalizedInput);
            var tokens = Tokenize(normalizedInput, expressionType);
            var isValid = ValidateTokens(tokens, expressionType);
            
            return new ParsedExpression
            {
                OriginalInput = input,
                NormalizedInput = normalizedInput,
                Type = expressionType,
                Tokens = tokens,
                IsValid = isValid,
                ErrorMessage = isValid ? null : "Invalid expression syntax"
            };
        }
        catch (Exception ex)
        {
            return new ParsedExpression
            {
                OriginalInput = input,
                NormalizedInput = input,
                Type = ExpressionType.Unknown,
                IsValid = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public CalculationResult? EvaluateExpression(string input)
    {
        var parsed = Parse(input);
        if (parsed == null || !parsed.IsValid)
            return CalculationResult.Error(parsed?.ErrorMessage ?? "Failed to parse expression", input);

        try
        {
            return parsed.Type switch
            {
                ExpressionType.Arithmetic => _mathEngine.Evaluate(input),
                ExpressionType.UnitConversion => _converter.Convert(input),
                ExpressionType.DateCalculation => _dateCalc.Calculate(input),
                ExpressionType.Logic => _logicEval.Evaluate(input),
                ExpressionType.NaturalLanguage => EvaluateNaturalLanguage(input),
                _ => CalculationResult.Error($"Unsupported expression type: {parsed.Type}", input)
            };
        }
        catch (Exception ex)
        {
            return CalculationResult.Error(ex.Message, input);
        }
    }

    private string NormalizeInput(string input)
    {
        input = input.Trim().ToLower();
        input = Regex.Replace(input, @"\s+", " ");
        return input;
    }

    private ExpressionType DetermineExpressionType(string input)
    {
        // Check for unit conversions (e.g., "10 km to miles", "5 feet inches")
        if (Regex.IsMatch(input, @"^\d+\.?\d*\s+[a-z²/]+\s+(?:to|in)\s+[a-z²/]+$") ||
            Regex.IsMatch(input, @"^\d+\.?\d*\s*[a-z²/]+\s+[a-z²/]+$"))
            return ExpressionType.UnitConversion;

        // Check for date calculations
        if (Regex.IsMatch(input, @"(today|yesterday|tomorrow|now|\d{4}[-/]\d{1,2}[-/]\d{1,2}|age of|days between|next|last|what day is)"))
            return ExpressionType.DateCalculation;

        // Check for logic operations
        if (Regex.IsMatch(input, @"(and|or|not|xor|&&|\|\||!|true|false|[&|^~]|<<|>>|==|!=|<|>|<=|>=)"))
            return ExpressionType.Logic;

        // Check for arithmetic (contains numbers and mathematical operators)
        if (Regex.IsMatch(input, @"\d+\.?\d*") && 
            (Regex.IsMatch(input, @"[+\-*/^%]") || 
             Regex.IsMatch(input, @"\b(sin|cos|tan|sqrt|log|ln|exp|abs|floor|ceil|round|factorial|pow)\b")))
            return ExpressionType.Arithmetic;

        // Default to natural language
        return ExpressionType.NaturalLanguage;
    }

    private List<Token> Tokenize(string input, ExpressionType type)
    {
        var tokens = new List<Token>();
        var patterns = GetTokenPatterns(type);
        var combinedPattern = string.Join("|", patterns.Select(p => $"({p})"));
        
        var matches = Regex.Matches(input, combinedPattern);
        int position = 0;
        
        foreach (Match match in matches)
        {
            var tokenType = DetermineTokenType(match.Value, type);
            tokens.Add(new Token
            {
                Type = tokenType,
                Value = match.Value,
                Position = position
            });
            position += match.Value.Length;
        }
        
        return tokens;
    }

    private string[] GetTokenPatterns(ExpressionType type)
    {
        return type switch
        {
            ExpressionType.Arithmetic => new[]
            {
                @"\d+\.?\d*", // Numbers
                @"[+\-*/^%]", // Operators
                @"\b(sin|cos|tan|asin|acos|atan|sqrt|cbrt|log|ln|log2|abs|exp|floor|ceil|round|factorial|pow)\b", // Functions
                @"\b(pi|e|tau|phi)\b", // Constants
                @"[(),]", // Parentheses and commas
                @"[a-zA-Z_]+" // Variables
            },
            ExpressionType.UnitConversion => new[]
            {
                @"\d+\.?\d*", // Numbers
                @"[a-z²/]+", // Units
                @"to|in" // Prepositions
            },
            ExpressionType.DateCalculation => new[]
            {
                @"\d{4}[-/]\d{1,2}[-/]\d{1,2}", // Dates
                @"\d{1,2}[-/]\d{1,2}[-/]\d{4}", // Alternative dates
                @"\d+\.?\d*", // Numbers
                @"(today|yesterday|tomorrow|now|monday|tuesday|wednesday|thursday|friday|saturday|sunday)", // Date keywords
                @"(day|days|week|weeks|month|months|year|years|hour|hours|minute|minutes|second|seconds|business days|workdays)", // Time units
                @"(age of|days between|next|last|what day is)", // Operations
                @"[+-]", // Operators
                @"ago|from now" // Relative time
            },
            ExpressionType.Logic => new[]
            {
                @"\d+\.?\d*", // Numbers
                @"(0b[01]+|0o[0-7]+|0x[0-9a-f]+)", // Different base numbers
                @"(true|false|1|0)", // Boolean values
                @"(and|or|not|xor|nand|nor|xnor)", // Word operators
                @"(&&|\|\||!)", // Symbolic operators
                @"[&|^~]|<<|>>", // Bitwise operators
                @"(==|!=|<>|<=|>=|<|>)", // Comparison operators
                @"[()]", // Parentheses
                @"[a-zA-Z_]+" // Variables
            },
            _ => new[] { @".+" } // Natural language - everything as one token
        };
    }

    private TokenType DetermineTokenType(string value, ExpressionType type)
    {
        if (type == ExpressionType.NaturalLanguage)
            return TokenType.Keyword;

        if (Regex.IsMatch(value, @"^\d+\.?\d*$"))
            return TokenType.Number;

        if (Regex.IsMatch(value, @"^(0b[01]+|0o[0-7]+|0x[0-9a-f]+)$"))
            return TokenType.Number;

        if (Regex.IsMatch(value, @"^(true|false|1|0)$"))
            return TokenType.Variable;

        if (Regex.IsMatch(value, @"^(sin|cos|tan|asin|acos|atan|sqrt|cbrt|log|ln|log2|abs|exp|floor|ceil|round|factorial|pow)$"))
            return TokenType.Function;

        if (Regex.IsMatch(value, @"^(pi|e|tau|phi)$"))
            return TokenType.Variable;

        if (Regex.IsMatch(value, @"^[+\-*/^%]$"))
            return TokenType.Operator;

        if (Regex.IsMatch(value, @"^[&|^~]|<<|>>$"))
            return TokenType.Operator;

        if (Regex.IsMatch(value, @"^(==|!=|<>|<=|>=|<|>)$"))
            return TokenType.Operator;

        if (Regex.IsMatch(value, @"^(and|or|not|xor|nand|nor|xnor|&&|\|\||!)$"))
            return TokenType.Operator;

        if (Regex.IsMatch(value, @"^[(),]$"))
            return value == "(" || value == ")" ? TokenType.Parenthesis : TokenType.Comma;

        if (type == ExpressionType.UnitConversion)
            return TokenType.Unit;

        if (Regex.IsMatch(value, @"^(today|yesterday|tomorrow|now|monday|tuesday|wednesday|thursday|friday|saturday|sunday)$"))
            return TokenType.Keyword;

        if (Regex.IsMatch(value, @"^(day|days|week|weeks|month|months|year|years|hour|hours|minute|minutes|second|seconds|business days|workdays)$"))
            return TokenType.Keyword;

        if (Regex.IsMatch(value, @"^(age of|days between|next|last|what day is|ago|from now|to|in)$"))
            return TokenType.Keyword;

        if (Regex.IsMatch(value, @"^[a-z²/]+$"))
            return type == ExpressionType.UnitConversion ? TokenType.Unit : TokenType.Variable;

        return TokenType.Unknown;
    }

    private bool ValidateTokens(List<Token> tokens, ExpressionType type)
    {
        if (tokens.Count == 0)
            return false;

        return type switch
        {
            ExpressionType.Arithmetic => ValidateArithmeticTokens(tokens),
            ExpressionType.UnitConversion => ValidateUnitConversionTokens(tokens),
            ExpressionType.DateCalculation => ValidateDateCalculationTokens(tokens),
            ExpressionType.Logic => ValidateLogicTokens(tokens),
            _ => true // Natural language is always considered valid at this level
        };
    }

    private bool ValidateArithmeticTokens(List<Token> tokens)
    {
        // Basic validation: check for balanced parentheses and valid token sequence
        var parenCount = 0;
        var expectingNumber = true;

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Variable:
                case TokenType.Function:
                    if (!expectingNumber)
                        return false;
                    expectingNumber = false;
                    break;

                case TokenType.Operator:
                    if (expectingNumber)
                        return false;
                    expectingNumber = true;
                    break;

                case TokenType.Parenthesis:
                    if (token.Value == "(")
                    {
                        if (!expectingNumber)
                            return false;
                        parenCount++;
                    }
                    else
                    {
                        if (expectingNumber)
                            return false;
                        parenCount--;
                        if (parenCount < 0)
                            return false;
                    }
                    break;

                case TokenType.Comma:
                    if (expectingNumber)
                        return false;
                    break;

                default:
                    return false;
            }
        }

        return parenCount == 0 && !expectingNumber;
    }

    private bool ValidateUnitConversionTokens(List<Token> tokens)
    {
        // Should be: number unit (to|in) unit OR number unit unit
        return tokens.Count switch
        {
            3 => tokens[0].Type == TokenType.Number && 
                   tokens[1].Type == TokenType.Unit && 
                   (tokens[2].Type == TokenType.Unit || tokens[2].Value == "to" || tokens[2].Value == "in"),
            4 => tokens[0].Type == TokenType.Number && 
                   tokens[1].Type == TokenType.Unit && 
                   (tokens[2].Value == "to" || tokens[2].Value == "in") &&
                   tokens[3].Type == TokenType.Unit,
            _ => false
        };
    }

    private bool ValidateDateCalculationTokens(List<Token> tokens)
    {
        // Date expressions are complex, so we'll do basic validation
        // More detailed validation happens in the DateCalc module
        return tokens.Count >= 2;
    }

    private bool ValidateLogicTokens(List<Token> tokens)
    {
        // Basic validation similar to arithmetic but with logic-specific rules
        var parenCount = 0;
        var expectingOperand = true;

        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                case TokenType.Variable:
                    if (!expectingOperand)
                        return false;
                    expectingOperand = false;
                    break;

                case TokenType.Operator:
                    if (token.Value == "!" || token.Value == "not" || token.Value == "~")
                    {
                        // Unary operators are always valid
                    }
                    else
                    {
                        // Binary operators
                        if (expectingOperand)
                            return false;
                        expectingOperand = true;
                    }
                    break;

                case TokenType.Parenthesis:
                    if (token.Value == "(")
                    {
                        if (!expectingOperand)
                            return false;
                        parenCount++;
                    }
                    else
                    {
                        if (expectingOperand)
                            return false;
                        parenCount--;
                        if (parenCount < 0)
                            return false;
                    }
                    break;

                default:
                    return false;
            }
        }

        return parenCount == 0 && !expectingOperand;
    }

    private CalculationResult? EvaluateNaturalLanguage(string input)
    {
        // For now, natural language evaluation is not implemented
        // This would integrate with AI services when enabled
        return CalculationResult.Error("Natural language processing requires AI integration", input);
    }
}

public class ParsedExpression
{
    public string OriginalInput { get; set; } = string.Empty;
    public string NormalizedInput { get; set; } = string.Empty;
    public ExpressionType Type { get; set; }
    public List<Token> Tokens { get; set; } = new();
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Token
{
    public TokenType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public int Position { get; set; }
}

public enum ExpressionType
{
    Unknown,
    Arithmetic,
    Logic,
    UnitConversion,
    DateCalculation,
    NaturalLanguage
}

public enum TokenType
{
    Number,
    Operator,
    Function,
    Variable,
    Unit,
    Parenthesis,
    Comma,
    Keyword,
    Unknown
}
