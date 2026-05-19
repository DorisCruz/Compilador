using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Prac1
{
    public partial class Form1 : Form
    {
        string archivo = null;
        string archivoback = null;
        StreamReader Leer;
        StreamWriter Escribir;
        int i_caracter;
        string elemento;
        int Numero_linea = 1;
        int N_error = 0;
        StringBuilder reporteFinal = new StringBuilder();
        int erroresSintacticos = 0;
        int llavesAbiertas = 0;
        int llavesCerradas = 0;
        bool dentroEstructura = false;
        bool dentroComentarioMulti = false;
        bool comentarioMultilineaAbierto = false;


        Dictionary<string, string> PalabrasReservadasEsp = new Dictionary<string, string>()
        {
           {"int","entero"},
           {"float","flotante"},
           {"char","caracter"},
           {"double","doble"},
           {"if","si"},
           {"else","sino"},
           {"while","mientras"},
           {"for","para"},
           {"return","retornar"},
           {"void","vacio"},
           {"main","principal"},
           {"include","incluir"},
           {"printf","imprimir"},
           {"scanf","leer"},
           {"switch","seleccionar"},
           {"case","caso"},
           {"default","defecto"},
           {"condicion","condicion"}
        };

        // Tabla de símbolos
        public class Simbolo
        {
            public string Nombre { get; set; }
            public string Tipo { get; set; }        // int, float, void, etc.
            public string Categoria { get; set; }   // Variable, Funcion, Parametro
            public string Ambito { get; set; }      // Global, o nombre de función
            public string Parametros { get; set; }  // Solo para funciones
            public int Linea { get; set; }
        }

        public class NodoArbol
        {
            public string Valor { get; set; }
            public NodoArbol Izquierdo { get; set; }
            public NodoArbol Derecho { get; set; }

            public NodoArbol(string valor)
            {
                Valor = valor;
                Izquierdo = null;
                Derecho = null;
            }
        }

        List<Simbolo> tablaSimbolos = new List<Simbolo>();
        Dictionary<string, double> valoresVariables = new Dictionary<string, double>();
        string ambitoActual = "Global";
        public Form1()
        {
            InitializeComponent();
        }

        private string TraducirCodigo(string codigo)
        {
            foreach (var palabra in PalabrasReservadasEsp)
            {
                string p = $@"\b{Regex.Escape(palabra.Key)}\b";
                codigo = Regex.Replace(codigo, p, palabra.Value);
            }
            return codigo;
        }


        private void nuevoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            archivo = null;
        }

        private void guardarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            if (archivo != null)
            {
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(richTextBox1.Text);
                }
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter Escribir = new StreamWriter(archivo))
                    {
                        Escribir.Write(richTextBox1.Text);
                    }
                }
            }
        }

        private void guardarComoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";

            if (VentanaGuardar.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaGuardar.FileName;
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(richTextBox1.Text);
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }

        private void abrirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog VentanaAbrir = new OpenFileDialog();
            VentanaAbrir.Filter = "Texto|*.c";

            if (VentanaAbrir.ShowDialog() == DialogResult.OK)
            {
                archivo = VentanaAbrir.FileName;
                using (StreamReader Leer = new StreamReader(archivo))
                {
                    richTextBox1.Text = Leer.ReadToEnd();
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

      
        private void AgregarLinea(string seccion, string mensaje)
        {
            if (!reporteFinal.ToString().Contains(seccion))
            {
                reporteFinal.AppendLine("\n" + seccion);
            }

            reporteFinal.AppendLine(mensaje);
        }


        private void guardar()
        {
            SaveFileDialog VentanaGuardar = new SaveFileDialog();
            VentanaGuardar.Filter = "Texto|*.c";
            if (archivo != null)
            {
                using (StreamWriter Escribir = new StreamWriter(archivo))
                {
                    Escribir.Write(richTextBox1.Text);
                }
            }
            else
            {
                if (VentanaGuardar.ShowDialog() == DialogResult.OK)
                {
                    archivo = VentanaGuardar.FileName;
                    using (StreamWriter Escribir = new StreamWriter(archivo))
                    {
                        Escribir.Write(richTextBox1.Text);
                    }
                }
            }
            Form1.ActiveForm.Text = "Mi Compilador - " + archivo;
        }


        private void VerificarPuntoYComa(string linea, int numLinea)
        {
            string l = linea.Trim();

            if (l.StartsWith("#include")) return;
            if (l.StartsWith("#")) return;

            // Líneas que no necesitan ;
            if (l.EndsWith(";") ||
                l.EndsWith("{") ||
                l.EndsWith("}") ||
                l.EndsWith(":") ||
                l.Length == 0 ||
                l.StartsWith("//") ||
                l.StartsWith("/*") ||
                l.StartsWith("*") ||
                l.StartsWith("if") ||
                l.StartsWith("else") ||
                l.StartsWith("for") ||
                l.StartsWith("while") ||
                l.StartsWith("switch") ||
                l.StartsWith("case") ||
                l.StartsWith("default") ||
                l.StartsWith("do") ||
                Regex.IsMatch(l, @"^(int|float|void|char|double)\s+\w+\s*\(")) // declaración de función
                return;

            // Si contiene = o es una expresión o declaración de variable, necesita ;
            if (Regex.IsMatch(l, @"^[a-zA-Z_]") || Regex.IsMatch(l, @"^\d"))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: falta ';' → {l}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
            }
        }
        // INCLUDE 
        private string AnalizarInclude(string l, int numLinea)
        {
            if (!Regex.IsMatch(l, @"^#include\s*(<\w+\.h>|""\w+\.h"")$"))
            {
                erroresSintacticos++;
                AgregarLinea("Directivas", $"- Error en línea {numLinea}: #include mal escrito");
                return "ERROR";
            }

            AgregarLinea("Directivas", $"- Include definido correctamente en línea {numLinea}");
            return "OK";
        }


        // IF 
        private void AnalizarIf(string linea, int numLinea)
        {
            if (!Regex.IsMatch(linea, @"^if\s*\(.*\)\s*\{?$"))
            {
                erroresSintacticos++;
                AgregarLinea("Estructuras de control",
                    $"Error sintáctico en línea {numLinea}: if mal formado → {linea}");
            }
            else
            {
                AgregarLinea("Estructuras de control",
                    $"If correcto (línea {numLinea})");
            }
        }


        // SWITCH 
        private string AnalizarSwitch(string linea, int numLinea)
        {
            string l = linea.Trim();

            if (!Regex.IsMatch(l, @"^switch\b"))
                return $"Error sintáctico en línea {numLinea}: switch mal formado → {linea}";

            int posOpen = l.IndexOf('(');
            if (posOpen < 0)
                return $"Error sintáctico en línea {numLinea}: falta '(' en switch → {linea}";

            int posClose = l.IndexOf(')', posOpen + 1);
            if (posClose < 0)
                return $"Error sintáctico en línea {numLinea}: falta ')' en switch → {linea}";

            string condicion = l.Substring(posOpen + 1, posClose - posOpen - 1).Trim();
            if (string.IsNullOrWhiteSpace(condicion))
                return $"Error sintáctico en línea {numLinea}: condición vacía en switch → {linea}";

            string resto = l.Substring(posClose + 1).Trim();

            if (resto.Length == 0)
            {
                return $"Switch correcto en línea {numLinea}";
            }

            if (resto.StartsWith("{"))
                return $"Switch correcto en línea {numLinea}";

            return $"Error sintáctico en línea {numLinea}: formato inesperado después del ')' en switch → {linea}";
        }



        private void AnalizarFor(string linea, int numLinea)
        {
            if (!Regex.IsMatch(linea, @"^for\s*\(.*;.*;.*\)\s*\{?$"))
            {
                erroresSintacticos++;
                AgregarLinea("Estructuras de control",
                    $"Error sintáctico en línea {numLinea}: for mal formado → {linea}");
            }
            else
            {
                AgregarLinea("Estructuras de control",
                    $"For correcto (línea {numLinea})");
            }
        }


        //  CICLOS 
        private string AnalizarCiclo(string l, int n)
        {
            if (Regex.IsMatch(l, @"^for\s*\(\s*condicion\s*\)\s*\{?$"))
            {
                if (!l.Contains("{"))
                    return $"Error sintáctico en línea {n}: for sin '{{'";
             
            }

            if (Regex.IsMatch(l, @"^while\s*\(\s*condicion\s*\)\s*\{?$"))
            {
                if (!l.Contains("{"))
                    return $"Error sintáctico en línea {n}: while sin '{{'";
                return $"While definido correctamente en linea {n}";
            }

            if (l.Equals("do") || l.Equals("do {"))
                return "Inicio do-while";

            if (Regex.IsMatch(l, @"^\}\s*while\s*\(\s*condicion\s*\)\s*;\s*$"))
                return "Fin do-while";

            return $"Error sintáctico en línea {n}: ciclo mal formado";
        }
        private void AnalisisLexico()
        {
            int erroresLexicos = 0;
            comentarioMultilineaAbierto = false;

            Regex regexTokens = new Regex(
                @"#|//.*|/\*[\s\S]*?\*/|""[^""]*""|<[^>]+>|\d+(\.\d+)?|[a-zA-Z_]\w*|[{}()\[\];,+\-*/=<>:]"
            );

            using (StreamWriter Escribir = new StreamWriter(
                Path.ChangeExtension(archivo, ".back")))
            {
                string[] lineas = richTextBox1.Text.Split('\n');

                for (int i = 0; i < lineas.Length; i++)
                {
                    string linea = lineas[i];
                    MatchCollection tokens = regexTokens.Matches(linea);

                    for (int j = 0; j < tokens.Count; j++)
                    {
                        string valor = tokens[j].Value;

                        // Librerías
                        if (valor.StartsWith("<") && valor.EndsWith(">"))
                        {
                            Escribir.WriteLine("<");
                            Escribir.WriteLine("libreria");
                            Escribir.WriteLine(">");
                            continue;
                        }

                        // Comentario simple
                        if (valor.StartsWith("//"))
                        {
                            Escribir.WriteLine("comentario");
                            continue;
                        }

                        // Comentario multilinea
                        if (valor.StartsWith("/*"))
                        {
                            Escribir.WriteLine("comentario_multilinea");
                            comentarioMultilineaAbierto = false;
                            continue;
                        }

                        // Número
                        if (Regex.IsMatch(valor, @"^\d+(\.\d+)?$"))
                        {
                            Escribir.WriteLine("Numero");
                            continue;
                        }

                        // Palabras reservadas
                        string[] reservadas = {
                    "int","float","if","else","for","while",
                    "switch","case","default","do","break","return"
                };

                        if (reservadas.Contains(valor))
                        {
                            Escribir.WriteLine(valor);
                            continue;
                        }

                        // Identificador o función
                        if (Regex.IsMatch(valor, @"^[a-zA-Z_]\w*$"))
                        {
                            bool esFuncion = j + 1 < tokens.Count && tokens[j + 1].Value == "(";

                            if (esFuncion)
                            {
                                Escribir.WriteLine("funcion");
                                Rtbx_salida.AppendText($"Función detectada: {valor}\n");
                                Rtbx_salida.ScrollToCaret();

                                // Tipo de retorno (token anterior)
                                string tipoRetorno = "void";
                                if (j > 0)
                                {
                                    string tokenAnterior = tokens[j - 1].Value;
                                    string[] tipos = { "int", "float", "void", "char", "double" };
                                    if (tipos.Contains(tokenAnterior))
                                        tipoRetorno = tokenAnterior;
                                }

                                // Extraer parámetros entre ( )
                                string parametros = "";
                                int k = j + 2;
                                while (k < tokens.Count && tokens[k].Value != ")")
                                {
                                    parametros += tokens[k].Value + " ";
                                    k++;
                                }
                                parametros = parametros.Trim();
                                if (string.IsNullOrEmpty(parametros)) parametros = "ninguno";

                                // Actualizar ámbito
                                ambitoActual = valor;

                                tablaSimbolos.Add(new Simbolo
                                {
                                    Nombre = valor,
                                    Tipo = tipoRetorno,
                                    Categoria = "Función",
                                    Ambito = "Global",
                                    Parametros = parametros,
                                    Linea = i + 1
                                });
                            }
                            else
                            {
                                Escribir.WriteLine("identificador");

                                string[] tiposC = { "int", "float", "char", "double", "void" };
                                if (j > 0 && tiposC.Contains(tokens[j - 1].Value))
                                {
                                    // Detectar si estamos dentro de paréntesis de función
                                    // buscando si hay un '(' antes en la misma línea sin cerrar
                                    bool esParametro = false;
                                    int abiertos = 0;
                                    for (int p = 0; p <= j; p++)
                                    {
                                        if (tokens[p].Value == "(") abiertos++;
                                        if (tokens[p].Value == ")") abiertos--;
                                    }
                                    esParametro = abiertos > 0;

                                    tablaSimbolos.Add(new Simbolo
                                    {
                                        Nombre = valor,
                                        Tipo = tokens[j - 1].Value,
                                        Categoria = esParametro ? "Parámetro" : "Variable",
                                        Ambito = ambitoActual,
                                        Parametros = "-",
                                        Linea = i + 1
                                    });
                                }
                            }

                            continue;
                        }

                        // Símbolos
                        Escribir.WriteLine(valor);
                    }

                    // salto de línea
                    Escribir.WriteLine("SL");
                }
            }

            Rtbx_salida.AppendText($"Errores léxicos: {erroresLexicos}\n");
        }

        private void VerificarParentesisFuncion(string linea, int numLinea)
        {
            string l = linea.Trim();

            // Solo analizar si tiene paréntesis (es declaración de función)
            if (!l.Contains("(")) return;  // <-- agrega esta línea

            if (Regex.IsMatch(l, @"^(int|float|void|char|double)\s+\w+"))
            {
                if (!l.Contains("(") || !l.Contains(")"))
                {
                    erroresSintacticos++;
                    string msg =
                        $"Error en línea {numLinea}: paréntesis incorrectos en función";

                    Rtbx_salida.AppendText(msg + "\n");
                    reporteFinal.AppendLine(msg);
                    return;
                }

                int ini = l.IndexOf("(");
                int fin = l.IndexOf(")");

                if (ini < fin)
                {
                    string parametros = l.Substring(ini + 1, fin - ini - 1).Trim();

                    if (parametros.Length == 0)
                        return; 

                    string[] lista = parametros.Split(',');

                    foreach (string p in lista)
                    {
                        string param = p.Trim();

                        // debe tener tipo y nombre
                        if (!Regex.IsMatch(param,
                            @"^(int|float|char|double)\s+[a-zA-Z_]\w*$"))
                        {
                            erroresSintacticos++;
                            string msg =
                                $"Error en línea {numLinea}: parámetro inválido → {param}";

                            Rtbx_salida.AppendText(msg + "\n");
                            reporteFinal.AppendLine(msg);
                        }
                    }
                }
            }
        }



        private void ActualizarEstadoEstructura(string linea)
        {
            if (linea.StartsWith("if") ||
                linea.StartsWith("for") ||
                linea.StartsWith("while") ||
                linea.StartsWith("switch") ||
                linea.StartsWith("do") ||
                linea.Contains("{"))
            {
                dentroEstructura = true;
            }

            if (linea.Contains("}"))
            {
                dentroEstructura = false;
            }
        }


        private void GenerarBack()
        {
            if (string.IsNullOrEmpty(archivo)) return;

            string archivoBack = Path.ChangeExtension(archivo, ".back");

            File.WriteAllText(archivoBack, richTextBox1.Text);
        }

        private void DetectarEstructuraMalEscrita(string linea, int numLinea)
        {
            if (Regex.IsMatch(linea, @"^[a-zA-Z_]\w*\s*=")) return;

            string[] estructuras = { "if", "else", "for", "while", "switch", "do" };
            string[] tipos = { "int ", "float ", "double ", "void ", "char " };

            string l = linea.TrimStart();

            foreach (string t in tipos)
                if (l.StartsWith(t))
                    return;

            string palabra = l.Split(' ', '(', '\t')[0];

            if (palabra.StartsWith("els") && palabra != "else")
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: palabra clave 'else' mal escrita → {palabra}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            if (palabra.StartsWith("else") && palabra != "else")
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: palabra clave 'else' mal escrita → {palabra}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            if (estructuras.Contains(palabra))
                return;

            if (!estructuras.Contains(palabra) && l.Contains("("))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: estructura de control mal escrita → {palabra}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
            }
        }

       

        private void AnalizarExpresion(string linea, int numLinea)
        {
            // Solo analizar líneas con asignación
            if (!linea.Contains("=")) return;
            if (linea.StartsWith("if") || linea.StartsWith("for") || linea.StartsWith("while")) return;

            // Verificar que haya una variable antes del =
            int posEqual = linea.IndexOf('=');
            if (posEqual > 0 && "!<>".Contains(linea[posEqual - 1])) return;
            if (posEqual + 1 < linea.Length && linea[posEqual + 1] == '=') return;
            string ladoIzquierdo = linea.Substring(0, posEqual).Trim();

            // Si es declaración con tipo como "int a", extraer solo el nombre
            string[] tiposC = { "int", "float", "char", "double", "void" };
            foreach (string tipo in tiposC)
            {
                if (ladoIzquierdo.StartsWith(tipo + " "))
                {
                    ladoIzquierdo = ladoIzquierdo.Substring(tipo.Length).Trim();
                    break;
                }
            }
            // Verificar que la variable del lado izquierdo esté definida
            var simboloIzq = tablaSimbolos.FirstOrDefault(s => s.Nombre == ladoIzquierdo.Trim());
            if (simboloIzq == null)
            {
                bool esDeclaracion = false;
                string[] tiposC2 = { "int", "float", "char", "double", "void" };
                foreach (string tipo in tiposC2)
                    if (linea.TrimStart().StartsWith(tipo + " ")) { esDeclaracion = true; break; }

                if (!esDeclaracion)
                {
                    erroresSintacticos++;
                    Rtbx_salida.AppendText($"Error: variable '{ladoIzquierdo.Trim()}' no definida en línea {numLinea}. Declárela primero con su tipo. Ejemplo: int {ladoIzquierdo.Trim()} = ...\n");
                    Rtbx_salida.AppendText("----------------------------------------\n");
                    return;
                }
            }
            else
            {
                // La variable existe pero verificar que no sea una asignación sin declaración previa
                // es decir que la declaración fue en la misma línea o antes
                bool esMismaLinea = linea.TrimStart().StartsWith("int ") ||
                                    linea.TrimStart().StartsWith("float ") ||
                                    linea.TrimStart().StartsWith("char ") ||
                                    linea.TrimStart().StartsWith("double ");

                if (!esMismaLinea && simboloIzq.Linea > numLinea)
                {
                    erroresSintacticos++;
                    Rtbx_salida.AppendText($"Error: variable '{ladoIzquierdo.Trim()}' usada antes de ser declarada en línea {numLinea}\n");
                    Rtbx_salida.AppendText("----------------------------------------\n");
                    return;
                }
            }

            if (!Regex.IsMatch(ladoIzquierdo, @"^[a-zA-Z_]\w*$"))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: falta variable antes del '=' → {linea}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            // Extraer la parte derecha de la asignación
            int posIgual = linea.IndexOf('=');

            // Evitar == 
            if (posIgual + 1 < linea.Length && linea[posIgual + 1] == '=') return;
            // Evitar !=, <=, >=
            if (posIgual > 0 && "!<>".Contains(linea[posIgual - 1])) return;

            string expresion = linea.Substring(posIgual + 1).Trim().TrimEnd(';');

            if (string.IsNullOrWhiteSpace(expresion)) return;

            // 1. Verificar paréntesis balanceados
            int abiertos = 0;
            foreach (char c in expresion)
            {
                if (c == '(') abiertos++;
                if (c == ')') abiertos--;
                if (abiertos < 0)
                {
                    erroresSintacticos++;
                    string msg = $"Error sintáctico en línea {numLinea}: ')' sin '(' en expresión → {expresion}";
                    Rtbx_salida.AppendText(msg + "\n");
                    reporteFinal.AppendLine(msg);
                    return;
                }
            }
            if (abiertos != 0)
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: paréntesis no balanceados en expresión → {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            // 2. Verificar operadores dobles, pero permitir ++ y --
            string exprTemp = expresion.Replace("++", "").Replace("--", "");
            // Quitar espacios para detectar operadores separados por espacios
            string exprSinEspaciosTemp = exprTemp.Replace(" ", "");
            if (Regex.IsMatch(exprSinEspaciosTemp, @"[+\-*/]{2,}"))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: operadores consecutivos en expresión → {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            // 3. Verificar que no empiece ni termine con operador
            char primero = expresion[0];
            char ultimo = expresion[expresion.Length - 1];
            if ("*/".Contains(primero))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: expresión empieza con operador '{primero}' → {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }
            if ("+-*/".Contains(ultimo))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: expresión termina con operador '{ultimo}' → {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            // 4. Verificar que los tokens sean válidos (números, identificadores, operadores, paréntesis)
            string exprSinEspacios = expresion.Replace(" ", "");
            if (!Regex.IsMatch(exprSinEspacios, @"^[a-zA-Z0-9_+\-*/().]+$"))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: caracteres inválidos en expresión → {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }
            if (Regex.IsMatch(exprSinEspacios, @"\)[a-zA-Z0-9_(]|[a-zA-Z0-9_]\("))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: falta operador entre operandos : {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }
            // Verificar que no haya operador antes de )  o después de (
            if (Regex.IsMatch(exprSinEspacios, @"[+\-*/]\)|[\(][+*/]"))
            {
                erroresSintacticos++;
                string msg = $"Error sintáctico en línea {numLinea}: operador sin operando en expresión → {expresion}";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                return;
            }

            // Si pasó todo, es correcta
            string ok = $"Expresión correcta en línea {numLinea}: {expresion}";
            Rtbx_salida.AppendText("\n" + ok + "\n");
            reporteFinal.AppendLine(ok);

            NodoArbol raiz = ConstruirArbol(expresion);
            if (raiz != null)
            {
                Rtbx_salida.AppendText($"Raíz del árbol: {raiz.Valor}\n");

                // Verificar que todas las variables de la expresión estén definidas
                List<string> tokens = TokenizarExpresion(expresion);
                foreach (string token in tokens)
                {
                    // Si el token es un identificador (no operador, no número)
                    if (Regex.IsMatch(token, @"^[a-zA-Z_]\w*$"))
                    {
                        var existe = tablaSimbolos.FirstOrDefault(s => s.Nombre == token);
                        if (existe == null)
                        {
                            erroresSintacticos++;
                            Rtbx_salida.AppendText($"Error: variable '{token}' no definida en línea {numLinea}\n");
                            Rtbx_salida.AppendText("----------------------------------------\n");
                            return;
                        }
                    }
                }



                // Guardar valor si es declaración con valor numérico
                try
                {
                    double valorInicial = EvaluarArbol(raiz);
                    if (!valoresVariables.ContainsKey(ladoIzquierdo.Trim()))
                        valoresVariables[ladoIzquierdo.Trim()] = valorInicial;
                }
                catch { }

                try
                {
                    double resultado = EvaluarArbol(raiz);
                    Rtbx_salida.AppendText($"Resultado: {resultado}\n");
                    string varNombre = ladoIzquierdo.Trim();
                    var simbolo = tablaSimbolos.FirstOrDefault(s => s.Nombre == varNombre);

                    if (simbolo != null)
                    {
                        bool tieneDecimal = resultado != Math.Floor(resultado);

                        if (simbolo.Tipo == "char")
                        {
                            erroresSintacticos++;
                            Rtbx_salida.AppendText($"Error de correspondencia: '{varNombre}' es char y no puede almacenar una expresión aritmética\n");
                        }
                        else if (simbolo.Tipo == "int" && tieneDecimal)
                        {
                            erroresSintacticos++;
                            Rtbx_salida.AppendText($"Error de correspondencia: '{varNombre}' es int pero el resultado {resultado} es decimal\n");
                        }
                        else if (simbolo.Tipo == "float" && !tieneDecimal)
                        {
                            Rtbx_salida.AppendText($"Advertencia: '{varNombre}' es float pero el resultado {resultado} es entero\n");
                        }
                    }
                }
                catch
                {
                    Rtbx_salida.AppendText("Resultado: no evaluable (contiene variables)\n");
                }

                Rtbx_salida.AppendText("----------------------------------------\n");
            }
        }
        

       
        private bool EsComentarioMultilinea(string linea)
        {
            string l = linea.Trim();

            // Si ya estamos dentro del comentario
            if (dentroComentarioMulti)
            {
                if (l.Contains("*/"))
                {
                    dentroComentarioMulti = false;
                }
                return true;
            }

            // Si empieza comentario multilínea
            if (l.Contains("/*"))
            {
                // Si no se cierra en la misma línea
                if (!l.Contains("*/"))
                {
                    dentroComentarioMulti = true;
                }
                return true;
            }

            // Comentario de una sola línea
            if (l.StartsWith("//"))
            {
                return true;
            }

            return false;
        }

        private void ExportarTablaCSV()
        {
            if (tablaSimbolos.Count == 0) return;

            string carpeta = Path.GetDirectoryName(archivo);

            // Archivo de funciones
            string rutaFunciones = Path.Combine(carpeta, "funciones.csv");
            using (StreamWriter sw = new StreamWriter(rutaFunciones))
            {
                sw.WriteLine("Nombre,Parámetros,Número de Parámetros");
                foreach (var s in tablaSimbolos.Where(x => x.Categoria == "Función"))
                {
                    int numParams = s.Parametros == "ninguno" ? 0 : s.Parametros.Split(',').Length;
                    sw.WriteLine($"{s.Nombre},{s.Parametros},{numParams}");
                }
            }

            // Archivo de variables y parámetros
            string rutaVariables = Path.Combine(carpeta, "variables.csv");
            using (StreamWriter sw = new StreamWriter(rutaVariables))
            {
                sw.WriteLine("Nombre,Tipo,Origen");
                foreach (var s in tablaSimbolos.Where(x => x.Categoria != "Función"))
                    sw.WriteLine($"{s.Nombre},{s.Tipo},{s.Ambito}");
            }

            Rtbx_salida.AppendText($"\nArchivos CSV generados en: {carpeta}\n");
            Rtbx_salida.AppendText($"- funciones.csv\n");
            Rtbx_salida.AppendText($"- variables.csv\n");
        }


        //Arbol binario
        private List<string> TokenizarExpresion(string expresion)
        {

            List<string> tokens = new List<string>();
            string actual = "";

            foreach (char c in expresion.Replace(" ", ""))
            {
                if ("+-*/()".Contains(c))
                {
                    if (actual.Length > 0)
                    {
                        tokens.Add(actual);
                        actual = "";
                    }
                    tokens.Add(c.ToString());
                }
                else
                {
                    actual += c;
                }
            }

            if (actual.Length > 0)
                tokens.Add(actual);

            return tokens;
        }

        private int ObtenerPrecedencia(string op)
        {
            if (op == "+" || op == "-") return 1;
            if (op == "*" || op == "/") return 2;
            return 0;
        }

        private NodoArbol ConstruirArbol(string expresion)
        {

            List<string> tokens = TokenizarExpresion(expresion);
            Stack<NodoArbol> operandos = new Stack<NodoArbol>();
            Stack<string> operadores = new Stack<string>();

            foreach (string token in tokens)
            {
                if (token == "(")
                {
                    operadores.Push(token);
                }
                else if (token == ")")
                {
                    while (operadores.Count > 0 && operadores.Peek() != "(")
                        AplicarOperador(operandos, operadores);
                    if (operadores.Count > 0)
                        operadores.Pop(); // quitar (
                }
                else if ("+-*/".Contains(token))
                {
                    while (operadores.Count > 0 &&
                           operadores.Peek() != "(" &&
                           ObtenerPrecedencia(operadores.Peek()) >= ObtenerPrecedencia(token))
                        AplicarOperador(operandos, operadores);

                    operadores.Push(token);
                }
                else
                {
                    operandos.Push(new NodoArbol(token));
                }
            }

            while (operadores.Count > 0)
                AplicarOperador(operandos, operadores);

            return operandos.Count > 0 ? operandos.Pop() : null;
        }

        private double EvaluarArbol(NodoArbol nodo)
        {
            if (nodo == null) throw new Exception("Nodo nulo");

            // Es un número
            if (double.TryParse(nodo.Valor, out double num))
                return num;

            // Es una variable con valor conocido
            if (valoresVariables.ContainsKey(nodo.Valor))
                return valoresVariables[nodo.Valor];

            // Es un operador
            double izq = EvaluarArbol(nodo.Izquierdo);
            double der = EvaluarArbol(nodo.Derecho);

            switch (nodo.Valor)
            {
                case "+": return izq + der;
                case "-": return izq - der;
                case "*": return izq * der;
                case "/":
                    if (der == 0) throw new DivideByZeroException();
                    return izq / der;
                default: throw new Exception("Operador desconocido");
            }
        }
        private void AplicarOperador(Stack<NodoArbol> operandos, Stack<string> operadores)
        {
            if (operandos.Count < 2 || operadores.Count == 0) return;

            string op = operadores.Pop();
            NodoArbol derecho = operandos.Pop();
            NodoArbol izquierdo = operandos.Pop();

            NodoArbol nodo = new NodoArbol(op);
            nodo.Izquierdo = izquierdo;
            nodo.Derecho = derecho;

            operandos.Push(nodo);
        }

        private void MostrarArbol(NodoArbol nodo, string prefijo, bool esIzquierdo)
        {
            if (nodo == null) return;

            Rtbx_salida.AppendText(prefijo + (esIzquierdo ? "├── " : "└── ") + nodo.Valor + "\n");

            string nuevoPrefijo = prefijo + (esIzquierdo ? "│   " : "    ");
            MostrarArbol(nodo.Izquierdo, nuevoPrefijo, true);
            MostrarArbol(nodo.Derecho, nuevoPrefijo, false);
        }



        //  BOTÓN ANALIZAR
        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reporteFinal.Clear();
            Rtbx_salida.Clear();
            tablaSimbolos.Clear();
            ambitoActual = "Global";
            erroresSintacticos = 0;
            N_error = 0;

            if (string.IsNullOrEmpty(archivo))
            {
                archivo = Path.Combine(Application.StartupPath, "temporal.c");
                File.WriteAllText(archivo, richTextBox1.Text);
            }

            guardar();
            Application.DoEvents();
            GenerarBack();
            AnalisisLexico();

            llavesAbiertas = 0;
            llavesCerradas = 0;

            string[] lineas = richTextBox1.Text.Split('\n');

            AgregarLinea("Resumen del análisis", "Errores léxicos: 0");

            for (int i = 0; i < lineas.Length; i++)
            {
                string l = lineas[i];

                if (string.IsNullOrWhiteSpace(l))
                    continue;

                int numLinea = i + 1;

                // Contar llaves
                llavesAbiertas += l.Count(c => c == '{');
                llavesCerradas += l.Count(c => c == '}');

                string lineaTrim = l.Trim();

                if (EsComentarioMultilinea(lineaTrim))
                    continue;

                // Detectar entrada a estructuras
                ActualizarEstadoEstructura(lineaTrim);
                AnalizarExpresion(lineaTrim, numLinea);


                // Verifica parentesis sin cerrar en funcion
                VerificarParentesisFuncion(lineaTrim, numLinea);

                // Verificar falta de ;
                VerificarPuntoYComa(lineaTrim, numLinea);
                

                if (lineaTrim.StartsWith("#include"))
                {
                    AnalizarInclude(lineaTrim, numLinea);
                }
                else if (lineaTrim.StartsWith("if"))
                {
                    AnalizarIf(lineaTrim, numLinea);
                }
                else if (lineaTrim.StartsWith("for"))
                {
                    AnalizarFor(lineaTrim, numLinea);
                }
                else if (lineaTrim.StartsWith("while"))
                {
                    AnalizarCiclo(lineaTrim, numLinea);
                }
                else if (lineaTrim.StartsWith("switch"))
                {
                    string res = AnalizarSwitch(lineaTrim, numLinea);
                    Rtbx_salida.AppendText(res + "\n");
                    reporteFinal.AppendLine(res);
                }

                DetectarEstructuraMalEscrita(lineaTrim, numLinea);

            }

            // Validar llaves
            if (llavesAbiertas != llavesCerradas)
            {
                string msg = $"\nError: faltan llaves. Abiertas: {llavesAbiertas}, Cerradas: {llavesCerradas}\n";
                Rtbx_salida.AppendText(msg);
                reporteFinal.AppendLine(msg);
                N_error++;
            }
            else
            {
                string msg = "\nLlaves balanceadas correctamente\n";
                Rtbx_salida.AppendText(msg);
                reporteFinal.AppendLine(msg);
            }

            // Validar comentario multilinea sin cerrar
            if (dentroComentarioMulti)
            {
                string msg = "Error sintáctico: comentario multilinea '/*' no cerrado.";
                Rtbx_salida.AppendText(msg + "\n");
                reporteFinal.AppendLine(msg);
                erroresSintacticos++;
            }


            // Total de errores
            string total = $"\nTotal de errores sintácticos y léxicos: {erroresSintacticos}";
            Rtbx_salida.AppendText(total);
            reporteFinal.AppendLine(total);

            ExportarTablaCSV();
        }


        // BOTÓN TRADUCIR 
        private void traducirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                string codigoTraducido = TraducirCodigo(richTextBox1.Text);

                if (archivo != null)
                {
                    string archivoTrad = archivo.Remove(archivo.Length - 1) + "trad";
                    using (StreamWriter escribirTrad = new StreamWriter(archivoTrad))
                    {
                        escribirTrad.Write(codigoTraducido);
                    }

                    MessageBox.Show("Archivo traducido generado: " + archivoTrad);
                }
                else
                {
                    MessageBox.Show("Guarda primero tu archivo para crear la traducción.");
                }
            }
            else
            {
                MessageBox.Show("No hay texto para traducir.");
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}

