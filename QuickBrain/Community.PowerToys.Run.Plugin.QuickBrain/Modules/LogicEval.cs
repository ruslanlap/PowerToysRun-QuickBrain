using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickBrain.Modules;

public class LogicEval
{
    private readonly Settings _settings;
    private readonly Dictionary<string, Func<bool[], bool>> _logicOperations;
    private readonly Dictionary<string, Func<long[], long>> _bitwiseOperations;
    private readonly Dictionary<string, Func<double[], bool>> _comparisonOperations;

    public LogicEval(Settings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logicOperations = InitializeLogicOperations();
        _bitwiseOperations = InitializeBitwiseOperations();
        _comparisonOperations = InitializeComparisonOperations();
    }

    private Dictionary<string, Func<bool[], bool>> InitializeLogicOperations()
    {
        return new Dictionary<string, Func<bool[], bool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["and"] = args => args[0] && args[1],
            ["&&"] = args => args[0] && args[1],
            ["or"] = args => args[0] || args[1],
            ["||"] = args => args[0] || args[1],
            ["xor"] = args => args[0] ^ args[1],
            ["nand"] = args => !(args[0] && args[1]),
            ["nor"] = args => !(args[0] || args[1]),
            ["xnor"] = args => !(args[0] ^ args[1]),
            ["not"] = args => !args[0],
            ["!"] = args => !args[0]
        };
    }

    private Dictionary<string, Func<long[], long>> InitializeBitwiseOperations()
    {
        return new Dictionary<string, Func<long[], long>>(StringComparer.OrdinalIgnoreCase)
        {
            ["&"] = args => args[0] & args[1],
            ["|"] = args => args[0] | args[1],
            ["^"] = args => args[0] ^ args[1],
            ["<<"] = args => args[0] << (int)args[1],
            [">>"] = args => args[0] >> (int)args[1],
            ["~"] = args => ~args[0]
        };
    }

    private Dictionary<string, Func<double[], bool>> InitializeComparisonOperations()
    {
        return new Dictionary<string, Func<double[], bool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["=="] = args => Math.Abs(args[0] - args[1]) < double.Epsilon,
            ["="] = args => Math.Abs(args[0] - args[1]) < double.Epsilon,
            ["!="] = args => Math.Abs(args[0] - args[1]) >= double.Epsilon,
            ["<>"] = args => Math.Abs(args[0] - args[1]) >= double.Epsilon,
            ["<"] = args => args[0] < args[1],
            ["<="] = args => args[0] <= args[1],
            [">"] = args => args[0] > args[1],
            [">="] = args => args[0] >= args[1]
        };
    }

    public CalculationResult? Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return CalculationResult.Error("Expression cannot be empty", expression);

        try
        {
            var normalizedExpression = NormalizeExpression(expression);

            // Check if this is a bitwise operation (contains &, |, ^, <<, >>, ~)
            if (ContainsBitwiseOperators(normalizedExpression))
            {
                var result = EvaluateBitwiseExpression(normalizedExpression);
                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"Result: {result} (0x{result:X})",
                    Result = result.ToString(),
                    RawExpression = expression,
                    Type = CalculationType.LogicEvaluation,
                    NumericValue = result,
                    IsError = false
                };
            }

            // Check if this is a comparison operation
            if (ContainsComparisonOperators(normalizedExpression))
            {
                var result = EvaluateComparisonExpression(normalizedExpression);
                return new CalculationResult
                {
                    Title = expression,
                    SubTitle = $"Result: {result}",
                    Result = result.ToString(),
                    RawExpression = expression,
                    Type = CalculationType.LogicEvaluation,
                    NumericValue = result ? 1 : 0,
                    IsError = false
                };
            }

            // Default to boolean logic evaluation
            var boolResult = EvaluateBooleanExpression(normalizedExpression);
            return new CalculationResult
            {
                Title = expression,
                SubTitle = $"Result: {boolResult}",
                Result = boolResult.ToString(),
                RawExpression = expression,
                Type = CalculationType.LogicEvaluation,
                NumericValue = boolResult ? 1 : 0,
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
        expression = Regex.Replace(expression, @"\s+", " ");
        return expression;
    }

    private bool ContainsBitwiseOperators(string expression)
    {
        return Regex.IsMatch(expression, @"[&|^~]|<<|>>");
    }

    private bool ContainsComparisonOperators(string expression)
    {
        return Regex.IsMatch(expression, @"(==|=|!=|<>|<=|>=|<|>)");
    }

    private long EvaluateBitwiseExpression(string expression)
    {
        // Parse numbers with different bases
        var numberPattern = @"(0b[01]+|0o[0-7]+|0x[0-9a-f]+|\d+)";
        var operatorPattern = @"([&|^]|<<|>>|~)";

        var tokens = Regex.Matches(expression, $@"{numberPattern}|{operatorPattern}|\(|\)")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (tokens.Count == 0)
            throw new ArgumentException("Invalid bitwise expression");

        // Handle unary NOT operator
        if (tokens[0] == "~")
        {
            if (tokens.Count < 2)
                throw new ArgumentException("Missing operand after ~ operator");

            var value = ParseNumber(tokens[1]);
            return ~value;
        }

        // Handle binary operations
        var stack = new Stack<long>();
        var operators = new Stack<string>();

        foreach (var token in tokens)
        {
            if (Regex.IsMatch(token, numberPattern))
            {
                stack.Push(ParseNumber(token));
            }
            else if (Regex.IsMatch(token, operatorPattern))
            {
                while (operators.Count > 0 && GetBitwisePrecedence(operators.Peek()) >= GetBitwisePrecedence(token))
                {
                    EvaluateBitwiseOperator(stack, operators.Pop());
                }
                operators.Push(token);
            }
            else if (token == "(")
            {
                operators.Push(token);
            }
            else if (token == ")")
            {
                while (operators.Count > 0 && operators.Peek() != "(")
                {
                    EvaluateBitwiseOperator(stack, operators.Pop());
                }
                if (operators.Count == 0)
                    throw new ArgumentException("Mismatched parentheses");
                operators.Pop(); // Remove "("
            }
        }

        while (operators.Count > 0)
        {
            EvaluateBitwiseOperator(stack, operators.Pop());
        }

        if (stack.Count != 1)
            throw new ArgumentException("Invalid bitwise expression");

        return stack.Pop();
    }

    private long ParseNumber(string numberStr)
    {
        if (numberStr.StartsWith("0b"))
            return Convert.ToInt64(numberStr.Substring(2), 2);
        if (numberStr.StartsWith("0o"))
            return Convert.ToInt64(numberStr.Substring(2), 8);
        if (numberStr.StartsWith("0x"))
            return Convert.ToInt64(numberStr.Substring(2), 16);

        return long.Parse(numberStr, CultureInfo.InvariantCulture);
    }

    private int GetBitwisePrecedence(string op)
    {
        return op switch
        {
            "~" => 4,
            "<<" or ">>" => 3,
            "&" => 2,
            "^" => 1,
            "|" => 0,
            _ => -1
        };
    }

    private void EvaluateBitwiseOperator(Stack<long> stack, string op)
    {
        if (op == "~")
        {
            if (stack.Count < 1)
                throw new ArgumentException("Insufficient operands for ~ operator");
            var a = stack.Pop();
            stack.Push(~a);
        }
        else
        {
            if (stack.Count < 2)
                throw new ArgumentException($"Insufficient operands for {op} operator");
            var b = stack.Pop();
            var a = stack.Pop();

            var result = op switch
            {
                "&" => a & b,
                "|" => a | b,
                "^" => a ^ b,
                "<<" => a << (int)b,
                ">>" => a >> (int)b,
                _ => throw new ArgumentException($"Unknown bitwise operator: {op}")
            };
            stack.Push(result);
        }
    }

    private bool EvaluateComparisonExpression(string expression)
    {
        var operators = new[] { "==", "!=", "<>", "<=", ">=", "<", ">" };

        foreach (var op in operators)
        {
            var parts = expression.Split(new[] { op }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var left = double.Parse(parts[0].Trim(), CultureInfo.InvariantCulture);
                var right = double.Parse(parts[1].Trim(), CultureInfo.InvariantCulture);
                return _comparisonOperations[op](new[] { left, right });
            }
        }

        throw new ArgumentException("Invalid comparison expression");
    }

    private bool EvaluateBooleanExpression(string expression)
    {
        // Replace word operators with symbols for easier parsing
        expression = expression.Replace(" and ", " && ");
        expression = expression.Replace(" or ", " || ");
        expression = expression.Replace(" not ", " ! ");
        expression = expression.Replace(" xor ", " ^ ");
        expression = expression.Replace(" nand ", " !& ");
        expression = expression.Replace(" nor ", " !| ");

        // Parse boolean values
        expression = expression.Replace("true", "1");
        expression = expression.Replace("false", "0");

        var tokens = Regex.Matches(expression, @"(\d+|&&|\|\||!|\^|\(|\))")
            .Cast<Match>()
            .Select(m => m.Value)
            .ToList();

        if (tokens.Count == 0)
            throw new ArgumentException("Invalid boolean expression");

        var stack = new Stack<bool>();
        var operators = new Stack<string>();

        foreach (var token in tokens)
        {
            if (token == "0" || token == "1")
            {
                stack.Push(token == "1");
            }
            else if (Regex.IsMatch(token, @"&&|\|\||!|\^"))
            {
                while (operators.Count > 0 && GetBooleanPrecedence(operators.Peek()) >= GetBooleanPrecedence(token))
                {
                    EvaluateBooleanOperator(stack, operators.Pop());
                }
                operators.Push(token);
            }
            else if (token == "(")
            {
                operators.Push(token);
            }
            else if (token == ")")
            {
                while (operators.Count > 0 && operators.Peek() != "(")
                {
                    EvaluateBooleanOperator(stack, operators.Pop());
                }
                if (operators.Count == 0)
                    throw new ArgumentException("Mismatched parentheses");
                operators.Pop(); // Remove "("
            }
        }

        while (operators.Count > 0)
        {
            EvaluateBooleanOperator(stack, operators.Pop());
        }

        if (stack.Count != 1)
            throw new ArgumentException("Invalid boolean expression");

        return stack.Pop();
    }

    private int GetBooleanPrecedence(string op)
    {
        return op switch
        {
            "!" or "not" => 3,
            "&&" or "and" => 2,
            "^" or "xor" => 1,
            "||" or "or" => 0,
            _ => -1
        };
    }

    private void EvaluateBooleanOperator(Stack<bool> stack, string op)
    {
        if (op == "!" || op == "not")
        {
            if (stack.Count < 1)
                throw new ArgumentException("Insufficient operands for NOT operator");
            var a = stack.Pop();
            stack.Push(!a);
        }
        else
        {
            if (stack.Count < 2)
                throw new ArgumentException($"Insufficient operands for {op} operator");
            var b = stack.Pop();
            var a = stack.Pop();

            var result = op switch
            {
                "&&" or "and" => a && b,
                "||" or "or" => a || b,
                "^" or "xor" => a ^ b,
                _ => throw new ArgumentException($"Unknown boolean operator: {op}")
            };
            stack.Push(result);
        }
    }
}
