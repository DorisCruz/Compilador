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

        List<Simbolo> tablaSimbolos = new List<Simbolo>();
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
            if (!dentroEstructura) return;

            string l = linea.Trim();

            if (l.StartsWith("#include"))
                return;

            if (l.EndsWith(";") ||
                l.EndsWith("{") ||
                l.EndsWith("}") ||
                l.EndsWith(":") ||
                l.StartsWith("if") ||
                l.StartsWith("for") ||
                l.StartsWith("while") ||
                l.StartsWith("switch") ||
                l.StartsWith("case") ||
                l.StartsWith("default") ||
                l.StartsWith("do") ||
                l.Contains("++") ||
                l.Contains("--"))
                return;

            erroresSintacticos++;
            string msg = $"Error sintáctico en línea {numLinea}: falta ';' → {l}";
            Rtbx_salida.AppendText(msg + "\n");
            reporteFinal.AppendLine(msg);
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


        //  BOTÓN ANALIZAR
        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reporteFinal.Clear();
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

            Rtbx_salida.Text = reporteFinal.ToString();

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

