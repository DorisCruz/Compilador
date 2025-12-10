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


        //   TOKEN: SIMBOLO
        private void Simbolo()
        {
            elemento = ((char)i_caracter).ToString();
        }

        //   TOKEN: CADENA "texto"
    
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

        //   TOKEN: NÚMERO  / REAL
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

        //   COMENTARIOS
        private bool Comentario()
        {
            if (i_caracter != '/') return false;

            i_caracter = Leer.Read();

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

        //   REPORTE DE ERROR LÉXICO
        private void Error(int c)
        {
            Rtbx_salida.AppendText("Error léxico: " +
                                   (char)c + " en línea " +
                                   Numero_linea + "\n");
            N_error++;
            i_caracter = Leer.Read();
        }

        //   CLASIFICADOR DE CARACTERES
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

        private void VerificarPuntoYComa(string linea, int numLinea)
        {
            if (!dentroEstructura) return;

            string l = linea.Trim();

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
                l.StartsWith("do"))
                return;

            erroresSintacticos++;
            string msg = $"Error sintáctico en línea {numLinea}: falta ';' → {l}";
            Rtbx_salida.AppendText(msg + "\n");
            reporteFinal.AppendLine(msg);
        }


        private string AnalizarLinea(string linea, int numLinea)
         {
             string l = linea.Trim();

             if (string.IsNullOrWhiteSpace(l)) return "";
             if (l.StartsWith("//") || l.StartsWith("/*") || l.StartsWith("*")) return "";

            // Contar llaves
            if (l == "{")
            {
                llavesAbiertas++;
                return "";
            }

            if (l == "}")
            {
                llavesCerradas++;
                return "";
            }


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


        // DECLARACIONES
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

        // ANÁLISIS LÉXICO 
        private void AnalisisLexico()
        {
            int erroresLexicos = 0;
            int numLinea = 1;

            Rtbx_salida.Clear();

            // Tokens válidos
            Regex simbolosInvalidos = new Regex(@"[^a-zA-Z0-9_{}\[\]\(\);\#\""<>\+\-\*/=:%\s,\.]");

            string[] lineas = richTextBox1.Text.Split('\n');

            foreach (string linea in lineas)
            {
                string l = linea.Trim();

                // Saltar líneas vacías y comentarios
                if (string.IsNullOrWhiteSpace(l) || l.StartsWith("//") || l.StartsWith("/*") || l.StartsWith("*"))
                {
                    numLinea++;
                    continue;
                }

                // Detectar símbolos inválidos
                Match m = simbolosInvalidos.Match(l);
                if (m.Success)
                {
                    Rtbx_salida.AppendText($"Error léxico en línea {numLinea}: carácter no válido '{m.Value}'\n");
                    erroresLexicos++;
                }

                // Validación básica de #include
                if (l.StartsWith("#include"))
                {
                    if (!Regex.IsMatch(l, @"^#include\s*(<\w+\.h>|""\w+\.h"")$"))
                    {
                        Rtbx_salida.AppendText(
                            $"Error léxico en línea {numLinea}: #include mal escrito → {linea}\n");
                        erroresLexicos++;
                    }
                }

                numLinea++;
            }

            Rtbx_salida.AppendText($"Errores léxicos: {erroresLexicos}\n");
            erroresSintacticos += erroresLexicos;
        }

        private void ActualizarEstadoEstructura(string linea)
        {
            if (linea.StartsWith("if") ||
                linea.StartsWith("for") ||
                linea.StartsWith("while") ||
                linea.StartsWith("switch") ||
                linea == "do" || linea == "do{")
            {
                dentroEstructura = true;
            }

            if (linea == "}")
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
            string[] validas = { "if", "for", "while", "switch", "do" };

            string l = linea.TrimStart();

            // Si tiene paréntesis, puede ser estructura
            if (l.Contains("("))
            {
                bool esValida = false;

                foreach (string v in validas)
                {
                    if (l.StartsWith(v + "(") || l.StartsWith(v + " ("))
                    {
                        esValida = true;
                        break;
                    }
                }

                if (!esValida)
                {
                    erroresSintacticos++;
                    string msg = $"Error sintáctico en línea {numLinea}: estructura mal escrita → {l}";
                    Rtbx_salida.AppendText(msg + "\n");
                    reporteFinal.AppendLine(msg);
                }
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


        //  BOTÓN ANALIZAR
        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {// Limpiar reportes y errores
            reporteFinal.Clear();
            erroresSintacticos = 0;
            N_error = 0;


            // ASEGURAR archivo físico (necesario para .back)
            if (string.IsNullOrEmpty(archivo))
            {
                archivo = Path.Combine(Application.StartupPath, "temporal.c");
                File.WriteAllText(archivo, richTextBox1.Text);
            }

            guardar();
            Application.DoEvents();
            GenerarBack();
            AnalisisLexico();


            // RESETEAR contadores de llaves
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

            // Total de errores
            string total = $"\nTotal de errores sintácticos y léxicos: {erroresSintacticos}";
            Rtbx_salida.AppendText(total);
            reporteFinal.AppendLine(total);

            Rtbx_salida.Text = reporteFinal.ToString();
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

