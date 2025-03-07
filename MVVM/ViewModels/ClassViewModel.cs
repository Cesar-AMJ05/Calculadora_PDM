using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Calculadora_PDM.MVVM.ViewModels
{

    [AddINotifyPropertyChangedInterface]
    public class CalcViewModel
    {
        public string Operation { get; set; } 
        public string Result { get; set; } = "=0";

        public string Memory { get; set; } = "0";

        public bool Flag { get; set; } = false;

        public ICommand OperationCommand =>
            new Command((number) => 
            { 
                Operation += number;
                Verification_Result();
            });

        public ICommand AllClearCommand =>
            new Command(() =>
            {
                Result = "0";
                Operation = "";
            });

        public ICommand MemoryCommand =>
             new Command(() =>
             {
                 Operation +=Memory;
                 Verification_Result();
             });

        public ICommand DeletleCommand =>
            new Command(() =>
            {
                if(Operation.Length > 0)
                {
                    Operation = Operation.Substring(0, Operation.Length - 1);
                    Verification_Result();
                }
            });

        public ICommand ResultCommand =>
            new Command(() =>
            {
                Verification_Result();
                Memory = Result;
                Operation = Result;
                Result = "";
            });

        public bool IsLastCharODigit(string input)
        {

            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            char last = input[input.Length - 1];
            return char.IsDigit(last);
        }

        public void Verification_Result()
        {
          
            try
            {
                double resultado = EvaluarOperacion(Operation);

                // Manejo de resultados especiales
                if (double.IsNaN(resultado))
                {
                    Result = "∞";
                }
                else if (double.IsPositiveInfinity(resultado))
                {
                    Result = "∞";
                }
                else if (double.IsNegativeInfinity(resultado))
                {
                    Result = "-∞";
                }
                else
                {
                    Result = resultado.ToString();
                }

                Flag = IsLastCharODigit(Operation);
                if (Flag)
                {
                    resultado = EvaluarOperacion(Operation);
                    Result = resultado.ToString();
                }
                else
                {
                    Result = "";
                }
            }
            catch (Exception ex)
            {
                Result = "---";
            }
        }
        /*Ya que estamos locos integramos un analisis lexico con un analisis sintactico
         * Tokenización y parsing
        */
        private double EvaluarOperacion(string expresion)
        {
            /*
             * Summary
             * Evalua una expresión matemática en notación infija y devuelve el resultado
             * 
             * <param name ="expresion"> La expresión matemática en formato string
             * 
             * <returns> El resultado *pain*
             */
            var tokens = Tokenizar(expresion);
            var rpn = Conv2Postfija(tokens);
            return EvaluarPostfija(rpn);
        }

        private List<string> Tokenizar(string expresion)
        {

            /*
             * Summary
             *  Convierte una expresión matemática en una lista de tokens.
             *
             * <param name ="expresion"> La expresión matemática en formato string
             * 
             * <returns> lista de tokens representando números y operadores
             */
            var tokens = new List<string>();
            string num = "";
            int i = 0;

            while (i < expresion.Length)
            {
                char c = expresion[i];

                if (c == '-' && (i == 0 || expresion[i - 1] == '(' || EsOperador(expresion[i - 1].ToString())))
                {
                    // Negativo unario
                    num = "-"; // Comienza el número con el signo negativo
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || c == '.')
                {
                    num += c;
                }
                else
                {
                    if (!string.IsNullOrEmpty(num))
                    {
                        tokens.Add(num);
                        num = "";
                    }
                    if (!char.IsWhiteSpace(c))
                    {
                        tokens.Add(c.ToString());
                    }
                }
                i++;
            }

            if (!string.IsNullOrEmpty(num))
            {
                tokens.Add(num);
            }

            return tokens;
        }

        private List<string> Conv2Postfija(List<string> tokens)
        {

            /*
             * Summary
             * Convierte una lista de tokens en notación infija a notación postfija (RPN) utilizando el algoritmo Shunting-Yard.
             * (La neta este si me lo revente del chat)
             * 
             * <param name ="tokens"> Lista de tokens en notacion infija 
             * 
             * <returns> Lista de tokens en notación postfija
             */

            var output = new List<string>();
            var operadores = new Stack<string>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (double.TryParse(token, out _))
                {
                    output.Add(token); // Números van directo a la salida
                }
                else if (token == "u-" || token == "%" || token == "√" || token == "⅟") // Operadores unarios
                {
                    operadores.Push(token);
                }
                else if (EsOperador(token))
                {
                    while (operadores.Count > 0 && Precedencia(operadores.Peek()) >= Precedencia(token))
                    {
                        output.Add(operadores.Pop());
                    }
                    operadores.Push(token);
                }
                else if (token == "(")
                {
                    operadores.Push(token);
                }
                else if (token == ")")
                {
                    while (operadores.Count > 0 && operadores.Peek() != "(")
                    {
                        output.Add(operadores.Pop());
                    }
                    operadores.Pop(); // Sacar el "("
                }
            }

            while (operadores.Count > 0)
            {
                output.Add(operadores.Pop());
            }

            return output;
        }

        private double EvaluarPostfija(List<string> rpn)
        {
            /*
             * Summary
             * Evalúa una expresión en notación postfija (RPN) y devuelve el resultado.
             * 
             * <param name ="rpn"> Lista de tokens en notacion postfija            
             * 
             * <returns> El resultado de la evaluacion
             */
            var pila = new Stack<double>();

            foreach (var token in rpn)
            {
                if (double.TryParse(token, out double numero))
                {
                    pila.Push(numero);
                }
                else if (token == "u-" || token == "√" || token == "⅟" || token == "%") // Operadores unarios
                {
                    double a = pila.Pop();
                    pila.Push(AplicarOperadorUnario(a, token));
                }
                else // Operadores binarios
                {
                    double b = pila.Pop();
                    double a = pila.Pop();
                    pila.Push(AplicarOperador(a, b, token));
                }
            }

            return pila.Pop();
        }

        private bool EsOperador(string token)
        {
            /*
             * Summary
             * Determina si el token es un operador
             * 
             * <param name ="token"> Token a evaluar
             * 
             * <returns> True si es un operador, false en caso contrario
             */
            return token == "+" || token == "-" || token == "*" || token == "/" || token == "^" || token == "%" || token == "⅟" || token == "√" || token == "u-";
        }

        private int Precedencia(string operador)
        {
            /*
             * Summary
             * Devuelve la precedencia de un operador matematico             
             * <param name ="operadoer"> Operador matematico a obtener la precedencia
             * 
             * <returns> Precedenciad del operador evaluado
             */
            switch (operador)
            {
                case "√":
                case "⅟":
                case "%":
                    return 5; // Máxima prioridad (operadores unarios)
                case "^":
                    return 4;
                case "*":
                case "/":
                    return 3;
                case "+":
                case "-":
                    return 2;
                default:
                    return 0;
            }
        }

        private double AplicarOperador(double a, double b, string operador)
        {
            /*
             * Summary
             * Aplica un operador matematico a dos operandos y devuelve el resuldado
             * <param name ="a"> Primer operado
             * <param name ="b"> Segundo operando
             * <param name ="operadoer"> Operador matematico a aplicar
             * 
             * <returns> El valor del resultado de la operacion
             */
            switch (operador)
            {
                case "+": return a + b;
                case "-": return a - b;
                case "*": return a * b;
                case "/":
                    if (b == 0)
                    {
                        if (a == 0)
                        {
                            return double.NaN; // 0/0 es NaN
                        }
                        else if (a > 0)
                        {
                            return double.PositiveInfinity; // 1/0 es +Infinity
                        }
                        else
                        {
                            return double.NegativeInfinity; // -1/0 es -Infinity
                        }
                    }
                    return a / b;
                case "^": return Math.Pow(a, b);
                default:
                    throw new ArgumentException($"Operador no válido: {operador}");
            }
        }

        private double AplicarOperadorUnario(double a, string operador)
        {
            /*
            * Summary
            * Aplica un operador matematico unario a operand y devuelve el resuldado (igual me lo revente del chat)
            * 
            * <param name ="a"> Primer operado
            * <param name ="operadoer"> Operador matematico a aplicar
            * 
            * <returns> El valor del resultado de la operacion
            */
            switch (operador)
            {
                case "u-": return -a;  // Negativo unario
                case "⅟":
                    if (a == 0)
                    {
                        return double.PositiveInfinity; // 1/0 es +Infinity
                    }
                    return 1 / a;  // Inverso 1/x
                case "√":
                    if (a < 0)
                    {
                        return double.NaN; // Raíz cuadrada de un número negativo es NaN
                    }
                    return Math.Sqrt(a);  // Raíz cuadrada
                case "%": return a / 100;  // Convertir a porcentaje
                default:
                    throw new ArgumentException($"Operador no válido: {operador}");
            }
        }

    }

}
