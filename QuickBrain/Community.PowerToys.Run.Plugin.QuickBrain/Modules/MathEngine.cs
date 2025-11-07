using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace QuickBrain.Modules;

public class MathEngine
{
    private readonly Settings _settings;
    private readonly Dictionary<string, Func<double[], double>> _functions;
    private readonly Dictionary<string, double> _constants;

    public MathEngine(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _functions = InitializeFunctions();
        _constants = InitializeConstants();
    }

    private Dictionary<string, Func<double[], double>> InitializeFunctions()
    {
        return new Dictionary<string, Func<double[], double>>(StringComparer.OrdinalIgnoreCase)
        {
            ["sin"] = args => Math.Sin(ConvertToRadians(args[0])),
            ["cos"] = args => Math.Cos(ConvertToRadians(args[0])),
            ["tan"] = args => Math.Tan(ConvertToRadians(args[0])),
            ["asin"] = args => ConvertFromRadians(Math.Asin(args[0])),
            ["acos"] = args => ConvertFromRadians(Math.Acos(args[0])),
            ["atan"] = args => ConvertFromRadians(Math.Atan(args[0])),
            ["sqrt"] = args => Math.Sqrt(args[0]),
            ["cbrt"] = args => Math.Cbrt(args[0]),
            ["log"] = args => Math.Log10(args[0]),
            ["ln"] = args => Math.Log(args[0]),
            ["log2"] = args => Math.Log2(args[0]),
            ["abs"] = args => Math.Abs(args[0]),
            ["exp"] = args => Math.Exp(args[0]),
            ["floor"] = args => Math.Floor(args[0]),
            ["ceil"] = args => Math.Ceiling(args[0]),
            ["round"] = args => Math.Round(args[0]),
            ["factorial"] = args => Factorial((int)args[0]),
            ["pow"] = args => Math.Pow(args[0], args[1])
        };
    }

    private Dictionary<string, double> InitializeConstants()
    {
        return new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["pi"] = Math.PI,
            ["e"] = Math.E,
            ["tau"] = 2 * Math.PI,
            ["phi"] = (1 + Math.Sqrt(5)) / 2
        };
    }

    private double ConvertToRadians(double angle)
    {
        return _settings.AngleUnit switch
        {
            AngleUnit.Degrees => angle * Math.PI / 180,
            AngleUnit.Gradians => angle * Math.PI / 200,
            _ => angle
        };
    }

    private double ConvertFromRadians(double radians)
    {
        return _settings.AngleUnit switch
        {
            AngleUnit.Degrees => radians * 180 / Math.PI,
            AngleUnit.Gradians => radians * 200 / Math.PI,
            _ => radians
        };
    }

    private double Factorial(int n)
    {
        if (n < 0) throw new ArgumentException("Factorial is not defined for negative numbers");
        if (n > 170) throw new OverflowException("Factorial result too large");
        
        double result = 1;
        for (int i = 2; i <= n; i++)
            result *= i;
        return result;
    }

    public CalculationResult? Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return CalculationResult.Error("Expression cannot be empty", expression);

        try
        {
            var normalizedExpression = NormalizeExpression(expression);
            var tokens = Tokenize(normalizedExpression);
            var rpn = ConvertToRPN(tokens);
            var result = EvaluateRPN(rpn);
            
            var formattedResult = FormatResult(result);
            
            return new CalculationResult
            {
                Title = expression,
                SubTitle = $"Result: {formattedResult}",
                Result = formattedResult,
                RawExpression = expression,
                Type = CalculationType.Arithmetic,
                NumericValue = result,
                IsError = false
            };
        }
        catch (Exception ex)
        {
            return CalculationResult.Error(ex.Message, expression);
        }
    }

    private string NormalizeExpression(string expression)
    {
        expression = expression.ToLower().Trim();
        expression = Regex.Replace(expression, @"\s+", "");
        
        // Replace constants
        foreach (var constant in _constants)
        {
            expression = Regex.Replace(expression, $@"\b{constant.Key}\b", constant.Value.ToString(CultureInfo.InvariantCulture));
        }
        
        // Handle implicit multiplication (e.g., "2pi" -> "2*pi")
        expression = Regex.Replace(expression, @"(\d)([a-z\(])", "$1*$2");
        expression = Regex.Replace(expression, @"(\))(\d)", "$1*$2");
        expression = Regex.Replace(expression, @"(\))(\()", "$1*$2");
        
        return expression;
    }

    private List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var pattern = @"(\d+\.?\d*|\.?\d+)|([+\-*/^%(),])|([a-z]+)";
        
        foreach (Match match in Regex.Matches(expression, pattern))
        {
            var value = match.Value;
            TokenType type;
            
            if (Regex.IsMatch(value, @"^\d+\.?\d*$|^\.\d+$"))
                type = TokenType.Number;
            else if (Regex.IsMatch(value, @"^[+\-*/^%(),]$"))
                type = value == "(" || value == ")" ? TokenType.Parenthesis : TokenType.Operator;
            else if (_functions.ContainsKey(value))
                type = TokenType.Function;
            else
                throw new ArgumentException($"Unknown token: {value}");
            
            tokens.Add(new Token { Type = type, Value = value });
        }
        
        return tokens;
    }

    private List<Token> ConvertToRPN(List<Token> tokens)
    {
        var output = new List<Token>();
        var operators = new Stack<Token>();
        
        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case TokenType.Number:
                    output.Add(token);
                    break;
                    
                case TokenType.Function:
                    operators.Push(token);
                    break;
                    
                case TokenType.Operator:
                    while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator &&
                           GetPrecedence(operators.Peek().Value) >= GetPrecedence(token.Value))
                    {
                        output.Add(operators.Pop());
                    }
                    operators.Push(token);
                    break;
                    
                case TokenType.Parenthesis:
                    if (token.Value == "(")
                        operators.Push(token);
                    else
                    {
                        while (operators.Count > 0 && operators.Peek().Value != "(")
                            output.Add(operators.Pop());
                        
                        if (operators.Count == 0)
                            throw new ArgumentException("Mismatched parentheses");
                        
                        operators.Pop(); // Remove "("
                        
                        if (operators.Count > 0 && operators.Peek().Type == TokenType.Function)
                            output.Add(operators.Pop());
                    }
                    break;
            }
        }
        
        while (operators.Count > 0)
        {
            var op = operators.Pop();
            if (op.Type == TokenType.Parenthesis)
                throw new ArgumentException("Mismatched parentheses");
            output.Add(op);
        }
        
        return output;
    }

    private int GetPrecedence(string op)
    {
        return op switch
        {
            "+" or "-" => 1,
            "*" or "/" or "%" => 2,
            "^" => 3,
            _ => 0
        };
    }

    private double EvaluateRPN(List<Token> rpn)
    {
        var stack = new Stack<double>();
        
        foreach (var token in rpn)
        {
            if (token.Type == TokenType.Number)
            {
                stack.Push(double.Parse(token.Value, CultureInfo.InvariantCulture));
            }
            else if (token.Type == TokenType.Operator)
            {
                if (stack.Count < 2)
                    throw new ArgumentException("Insufficient operands for operator");
                
                var b = stack.Pop();
                var a = stack.Pop();
                var result = token.Value switch
                {
                    "+" => a + b,
                    "-" => a - b,
                    "*" => a * b,
                    "/" => b == 0 ? throw new DivideByZeroException("Division by zero") : a / b,
                    "%" => b == 0 ? throw new DivideByZeroException("Division by zero") : a % b,
                    "^" => Math.Pow(a, b),
                    _ => throw new ArgumentException($"Unknown operator: {token.Value}")
                };
                stack.Push(result);
            }
            else if (token.Type == TokenType.Function)
            {
                var func = _functions[token.Value];
                var argCount = GetFunctionArgumentCount(token.Value);
                
                if (stack.Count < argCount)
                    throw new ArgumentException($"Insufficient arguments for function {token.Value}");
                
                var args = new double[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                    args[i] = stack.Pop();
                
                try
                {
                    var result = func(args);
                    stack.Push(result);
                }
                catch (Exception ex)
                {
                    throw new ArgumentException($"Error in function {token.Value}: {ex.Message}");
                }
            }
        }
        
        if (stack.Count != 1)
            throw new ArgumentException("Invalid expression");
        
        return stack.Pop();
    }

    private int GetFunctionArgumentCount(string functionName)
    {
        return functionName.ToLower() switch
        {
            "pow" => 2,
            _ => 1
        };
    }

    private string FormatResult(double result)
    {
        if (double.IsInfinity(result))
            return "Infinity";
        if (double.IsNaN(result))
            return "NaN";
        
        // Use scientific notation for very small or very large numbers
        if (Math.Abs(result) > 0 && (Math.Abs(result) < 0.0001 || Math.Abs(result) > 1e10))
        {
            return result.ToString("0.####E+0", CultureInfo.InvariantCulture);
        }
        
        int decimals = Math.Max(0, _settings.Precision);
        var rounded = Math.Round(result, decimals, MidpointRounding.AwayFromZero);
        var format = decimals == 0 ? "0" : "0." + new string('#', decimals);
        return rounded.ToString(format, CultureInfo.InvariantCulture);
    }
}
