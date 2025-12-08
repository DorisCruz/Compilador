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


        // ========= PALABRAS RESERVADAS =========
        List<string> P_Reservadas = new List<string>() {
            "int", "float", "char", "double",
            "if", "else", "while", "for",
            "return", "void", "main",
            "include", "printf", "scanf",
            "switch", "case", "default",
            "condicion"
        };

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

        // =================== MENÚS ===================

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


        // ======================================
        //   TOKEN: SIMBOLO
        // ======================================
        private void Simbolo()
        {
            elemento = ((char)i_caracter).ToString();
        }

        // ======================================
        //   TOKEN: CADENA "texto"
        // ======================================
        private void Cadena()
        {
            string token = "\"";
            i_caracter = Leer.Read();

            while (i_caracter != 34 && i_caracter != -1)
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            token += "\"";
            elemento = token;
        }

        // ======================================
        //   TOKEN: CARACTER 'A'
        // ======================================
        private void Caracter()
        {
            string token = "'";
            i_caracter = Leer.Read();

            if (i_caracter != -1 && i_caracter != '\'')
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            }

            if (i_caracter == 39)
            {
                token += "'";
            }
            else
            {
                Error(39);
            }

            elemento = token;
        }

        // ======================================
        //   TOKEN: IDENTIFICADOR / PALABRA RESERVADA
        // ======================================
        private void Identificador()
        {
            string token = "";

            do
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            }
            while (Tipo_caracter(i_caracter) == 'l' ||
                   Tipo_caracter(i_caracter) == 'd');

            elemento = token;
        }

        // ======================================
        //   TOKEN: NÚMERO  / REAL
        // ======================================
        private void Numero()
        {
            string token = "";

            do
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            }
            while (Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                token += ".";
                i_caracter = Leer.Read();
                while (Tipo_caracter(i_caracter) == 'd')
                {
                    token += (char)i_caracter;
                    i_caracter = Leer.Read();
                }
            }

            elemento = token;
        }

        // ======================================
        //   COMENTARIOS //  O  /*   */
        // ======================================
        private bool Comentario()
        {
            if (i_caracter != '/') return false;

            i_caracter = Leer.Read();

            // ---------- COMENTARIO DE LINEA ----------
            if (i_caracter == '/')
            {
                string comentario = "//";

                while (i_caracter != '\n' && i_caracter != -1)
                {
                    comentario += (char)i_caracter;
                    i_caracter = Leer.Read();
                }

                elemento = comentario;
                return true;
            }

            // ---------- COMENTARIO MULTILINEA ----------
            if (i_caracter == '*')
            {
                string comentario = "/*";
                bool finComentario = false;

                while (!finComentario && i_caracter != -1)
                {
                    i_caracter = Leer.Read();
                    comentario += (char)i_caracter;

                    if (i_caracter == '\n') Numero_linea++;

                    if (i_caracter == '*')
                    {
                        i_caracter = Leer.Read();
                        comentario += (char)i_caracter;

                        if (i_caracter == '/') finComentario = true;
                    }
                }

                elemento = comentario;
                return true;
            }

            return false;
        }

        // ======================================
        //   REPORTE DE ERROR LÉXICO
        // ======================================
        private void Error(int c)
        {
            Rtbx_salida.AppendText("Error léxico: " +
                                   (char)c + " en línea " +
                                   Numero_linea + "\n");
            N_error++;
            i_caracter = Leer.Read();
        }

        // ======================================
        //   CLASIFICADOR DE CARACTERES
        // ======================================
        private char Tipo_caracter(int c)
        {
            if (char.IsLetter((char)c)) return 'l';
            if (char.IsDigit((char)c)) return 'd';

            switch (c)
            {
                case 10: return 'n';     // salto de línea
                case 34: return '"';     // "
                case 39: return 'c';     // '
                case 32: return 'e';     // espacio
                default: return 's';     // símbolo
            }
        }

        private void AgregarLinea(string seccion, string mensaje)
        {
            if (!reporteFinal.ToString().Contains(seccion))
            {
                reporteFinal.AppendLine("\n" + seccion);
            }

            reporteFinal.AppendLine(mensaje);
        }

        //  VERIFICACIÓN DE #include
        private void VerificarDirectivaInclude()
        {
            if (archivo == null) return;

            int lineaNum = 1;

            Regex patronInclude = new Regex(@"^\s*#\s*include\s*(<\w+\.h>|""\w+\.h"")\s*$");
            Regex patronDirective = new Regex(@"^\s*#\s*(\w+)", RegexOptions.Compiled);

            using (StreamReader sr = new StreamReader(archivo))
            {
                string linea;

                while ((linea = sr.ReadLine()) != null)
                {
                    string trim = linea.TrimStart();

                    if (trim.StartsWith("#"))
                    {
                        Match m = patronDirective.Match(trim);

                        if (m.Success)
                        {
                            string nombre = m.Groups[1].Value;

                            if (nombre == "include")
                            {
                                if (!patronInclude.IsMatch(trim))
                                {
                                    Rtbx_salida.AppendText($"Error en directiva include en línea {lineaNum}: {linea}\n");
                                    N_error++;
                                }
                            }
                            else
                            {
                                Rtbx_salida.AppendText($"Directiva desconocida en línea {lineaNum}: {linea}\n");
                                N_error++;
                            }
                        }
                    }

                    lineaNum++;
                }
            }
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

        
        private void VerificarDeclaraciones()
        {
            if (archivo == null) return;

            int numLinea = 1;

            using (StreamReader sr = new StreamReader(archivo))
            {
                string linea;

                while ((linea = sr.ReadLine()) != null)
                {
                    string resultado = AnalizarLinea(linea, numLinea);

                    if (!string.IsNullOrEmpty(resultado))
                    {
                        Rtbx_salida.AppendText(resultado + "\n");

                        if (resultado.Contains("Error"))
                            N_error++;
                    }

                    numLinea++;
                }
            }
        }

        private string AnalizarLinea(string linea, int numLinea)
         {
             string l = linea.Trim();

             if (string.IsNullOrWhiteSpace(l)) return "";
             if (l.StartsWith("//") || l.StartsWith("/*") || l.StartsWith("*")) return "";

             // Aceptar llaves sin error
             if (l == "{" || l == "}")
                return "";

             // Aceptar break;
             if (l == "break;" || l == "continue;")
                return "";

             // Aceptar return
             if (l.StartsWith("return"))
                return "";

             // Asignaciones simples
             if (Regex.IsMatch(l, @"^[a-zA-Z_]\w*\s*=\s*.+;"))
                return "";

             if (l.StartsWith("#include"))
                return AnalizarInclude(l, numLinea);

            if (Regex.IsMatch(l, @"^(int|float|char|double)\s+"))
                return AnalizarDeclaracion(l, numLinea);

            if (l.StartsWith("if"))
            {
                AnalizarIf(l, numLinea);
                return "";
            }


            if (l.StartsWith("else"))
                return "Línea válida: else";

             if (l.StartsWith("switch"))
                return AnalizarSwitch(l, numLinea);

             if (l.StartsWith("case") || l.StartsWith("default"))
                return AnalizarCase(l, numLinea);

             if (l.StartsWith("for") || l.StartsWith("while") || 
                 l.StartsWith("do") || l.StartsWith("} while"))
                 return AnalizarCiclo(l, numLinea);

            return $"Error sintáctico en línea {numLinea}: instrucción no reconocida → {l}";
        }

        private void AnalizarEstructura(string linea, int numLinea)
        {
            if (linea.StartsWith("if"))
            {
                if (Regex.IsMatch(linea, @"^if\s*\(\s*condicion\s*\)\s*\{?$"))
                    Rtbx_salida.AppendText($"If correcto en línea {numLinea}\n");
                else
                {
                    Rtbx_salida.AppendText($"Error en línea {numLinea}: if mal formado → {linea}\n");
                    N_error++;
                }
                return;
            }

            if (linea == "else" || linea == "else{")
            {
                Rtbx_salida.AppendText($"Else correcto en línea {numLinea}\n");
                return;
            }

            if (linea.StartsWith("while"))
            {
                if (Regex.IsMatch(linea, @"^while\s*\(\s*condicion\s*\)\s*\{?$"))
                    Rtbx_salida.AppendText($"While correcto en línea {numLinea}\n");
                else
                {
                    Rtbx_salida.AppendText($"Error en línea {numLinea}: while mal formado → {linea}\n");
                    N_error++;
                }
                return;
            }

            if (linea.StartsWith("for"))
            {
                if (Regex.IsMatch(linea, @"^for\s*\(\s*condicion\s*\)\s*\{?$"))
                    Rtbx_salida.AppendText($"For correcto en línea {numLinea}\n");
                else
                {
                    Rtbx_salida.AppendText($"Error en línea {numLinea}: for mal formado → {linea}\n");
                    N_error++;
                }
                return;
            }

            if (linea.StartsWith("switch"))
            {
                if (Regex.IsMatch(linea, @"^switch\s*\(\s*condicion\s*\)\s*\{?$"))
                    Rtbx_salida.AppendText($"Switch correcto en línea {numLinea}\n");
                else
                {
                    Rtbx_salida.AppendText($"Error en línea {numLinea}: switch mal formado → {linea}\n");
                    N_error++;
                }
                return;
            }

            if (Regex.IsMatch(linea, @"^case\s+\d+\s*:"))
            {
                Rtbx_salida.AppendText($"Case correcto en línea {numLinea}\n");
                return;
            }

            if (linea.StartsWith("default"))
            {
                Rtbx_salida.AppendText($"Default correcto en línea {numLinea}\n");
                return;
            }

            if (linea == "do" || linea == "do{")
            {
                Rtbx_salida.AppendText($"Inicio do-while correcto en línea {numLinea}\n");
                return;
            }

            if (Regex.IsMatch(linea, @"^\}\s*while\s*\(\s*condicion\s*\)\s*;?$"))
            {
                Rtbx_salida.AppendText($"Fin do-while correcto en línea {numLinea}\n");
                return;
            }
        }


        private void AnalizarEstructurasControl()
        {
            if (archivo == null) return;

            int numLinea = 1;

            using (StreamReader sr = new StreamReader(archivo))
            {
                string linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    string l = linea.Trim();

                    if (string.IsNullOrWhiteSpace(l) ||
                        l.StartsWith("//") ||
                        l.StartsWith("/*") ||
                        l.StartsWith("*"))
                    {
                        numLinea++;
                        continue;
                    }

                    AnalizarEstructura(l, numLinea);
                    numLinea++;
                }
            }
        }


        // ======================= INCLUDE =======================
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


        // ===================== DECLARACIONES ====================
        private string AnalizarDeclaracion(string l, int n)
        {
            if (!Regex.IsMatch(l, @"^(int|float|char|double)\s+[a-zA-Z_]\w*(\s*=\s*[^;]+)?;$"))
                return $"Error sintáctico en línea {n}: declaración incorrecta";

            return $"Declaración válida en linea {n}";
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
        private string AnalizarSwitch(string l, int n)
        {
            if (!Regex.IsMatch(l, @"^switch\s*\(\s*condicion\s*\)\s*\{?$"))
                return $"Error sintáctico en línea {n}: switch mal formado";

            return "Switch correcto";
        }

        // CASE / DEFAULT
        private string AnalizarCase(string l, int n)
        {
            if (l.StartsWith("case"))
            {
                if (!Regex.IsMatch(l, @"^case\s+\d+\s*:\s*$"))
                    return $"Error sintáctico en línea {n}: case mal formado";

                return "Case correcto";
            }

            if (l.StartsWith("default"))
            {
                if (!Regex.IsMatch(l, @"^default\s*:\s*$"))
                    return $"Error sintáctico en línea {n}: default mal formado";

                return "Default correcto";
            }

            return "";
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


        // ========================= CICLOS =======================
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

        // ANÁLISIS LÉXICO COMPLETO
        private void AnalisisLexico()
        {
            elemento = "";
            Numero_linea = 1;
            int erroresLexicos = 0;

            archivoback = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoback);
            Leer = new StreamReader(archivo);

            // Tokens válidos
            Regex tokenRegex = new Regex(
                @"#|include|<[^>]+>|""[^""]+""|[a-zA-Z_]\w*|\d+|[{}()\[\];,+\-*/=%<>]|//.*|/\*.*?\*/",
                RegexOptions.Compiled | RegexOptions.Singleline
            );

            // Símbolos inválidos
            Regex simbolosInvalidos = new Regex(@"[^a-zA-Z0-9_{}\[\]\(\);\#\""<>\+\-\*/=:%\s,\.]");

            // #include válido
            Regex includeValido = new Regex(@"#\s*include\s*(<\w+\.h>|""\w+\.h"")");

            string linea;

            while ((linea = Leer.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(linea))
                {
                    Escribir.WriteLine("SL");
                }
                else
                {
                    string trimmed = linea.TrimStart();

                    // Validar includes mal escritos
                    if (trimmed.StartsWith("#include"))
                    {
                        if (!includeValido.IsMatch(trimmed))
                        {
                            Rtbx_salida.AppendText($"Error léxico en línea {Numero_linea}: librería mal escrita - {linea}\n");
                            erroresLexicos++;
                        }

                        // Sustituir <loquesea.h> por "libreria"
                        linea = Regex.Replace(linea, @"<[^>]+>|""[^""]+""", "libreria");
                    }

                    // Tokenizar
                    MatchCollection tokens = tokenRegex.Matches(linea);

                    foreach (Match token in tokens)
                    {
                        string valor = token.Value.Trim();

                        if (string.IsNullOrWhiteSpace(valor))
                            continue;

                        Escribir.WriteLine(valor);
                    }

                    // Símbolos ilegales
                    if (!trimmed.StartsWith("//") && !trimmed.Contains("/*"))
                    {
                        Match m = simbolosInvalidos.Match(linea);
                        if (m.Success)
                        {
                            Rtbx_salida.AppendText($"Error léxico en línea {Numero_linea}: carácter no válido '{m.Value}'\n");
                            erroresLexicos++;
                        }
                    }
                }

                Numero_linea++;
            }

            Escribir.Close();
            Leer.Close();

            Rtbx_salida.AppendText($"Errores léxicos: {erroresLexicos}\n");
            N_error += erroresLexicos;
        }

        // =========================================================
        //      BOTÓN ANALIZAR  (LEX + SINTÁCTICO)
        // =========================================================
        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reporteFinal.Clear();
            erroresSintacticos = 0;
            N_error = 0;

            string[] lineas = richTextBox1.Text.Split('\n');

            AgregarLinea("Resumen del análisis", "Errores léxicos: 0");

            for (int i = 0; i < lineas.Length; i++)
            {
                string l = lineas[i].Trim();
                int numLinea = i + 1;

                if (string.IsNullOrWhiteSpace(l))
                    continue;

                if (l.StartsWith("#include"))
                    AnalizarInclude(l, numLinea);
                else if (l.StartsWith("if"))
                    AnalizarIf(l, numLinea);
                else if (l.StartsWith("for"))
                    AnalizarFor(l, numLinea);
                else if (l.StartsWith("while"))
                    AnalizarCiclo(l, numLinea);
                else if (l.StartsWith("switch"))
                    AnalizarSwitch(l, numLinea);
            }

            reporteFinal.AppendLine($"\nTotal de errores sintácticos y léxicos: {erroresSintacticos}");

            Rtbx_salida.Text = reporteFinal.ToString();
        }


        // =========================================================
        //      BOTÓN TRADUCIR (.trad)
        // =========================================================
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

        // =========================================================
        //  EVENTO RICHTEXTBOX (NO SE USA)
        // =========================================================
        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
        }
    }
}

