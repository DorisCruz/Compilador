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
        string elemento;
        
        List<string> P_Reservadas = new List<string>() {
            "int", "float", "char", "double", "if", "else",
            "while", "for", "return", "void", "main",
            "include", "printf", "scanf", "switch", "case"
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
           {"case","caso"}
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


        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
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

        private char Tipo_caracter(int caracter)
        {
            if (caracter >= 65 && caracter <= 90 || caracter >= 97 && caracter <= 122) { return 'l'; } 
            else
            {
                if (caracter >= 48 && caracter <= 57) { return 'd'; } 
                else
                {
                    switch (caracter)
                    {
                        case 10: return 'n'; 
                        case 34: return '"';
                        case 39: return 'c';
                        case 32: return 'e';


                        default: return 's';
                    }
                    ;

                }
            }

        }

        private void Simbolo()
        {
            elemento = ((char)i_caracter).ToString();
        }
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

        private void Error(int i_caracter)
        {
            Rtbx_salida.AppendText("Error léxico: " + (char)i_caracter + " en línea " + Numero_linea + "\n");
            N_error++;
            i_caracter = Leer.Read();
        }
        private void Archivo_Libreria()
        {
            i_caracter = Leer.Read();
            if ((char)i_caracter == 'h') { elemento = "Archivo Libreria\n"; i_caracter = Leer.Read(); }
            else { Error(i_caracter); }
        }
        private bool Palabra_Reservada()
        {
            if (P_Reservadas.IndexOf(elemento) >= 0) return true;
            return false;
        }
        private void Identificador()
        {
            string token = "";

            // leer todas las letras o dígitos que forman el identificador
            do
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');

            // si es palabra reservada, igual se escribe tal cual (ej: int)
            elemento = token;
        }

        private void Numero_Real()
        {
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');
            elemento = "numero_real\n";
        }
        private void Numero()
        {
            string token = "";

            do
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');

            if ((char)i_caracter == '.')
            {
                token += (char)i_caracter;
                i_caracter = Leer.Read();

                while (Tipo_caracter(i_caracter) == 'd')
                {
                    token += (char)i_caracter;
                    i_caracter = Leer.Read();
                }
            }

            elemento = token;
        }

        private bool Comentario()
        {
            if (i_caracter != '/') return false;

            i_caracter = Leer.Read();

            if (i_caracter == '/')
            {
                while (i_caracter != '\n' && i_caracter != -1)
                    i_caracter = Leer.Read();
                return true;
            }

            if (i_caracter == '*')
            {
                bool finComentario = false;
                while (!finComentario && i_caracter != -1)
                {
                    i_caracter = Leer.Read();
                    if (i_caracter == '\n') Numero_linea++;

                    if (i_caracter == '*')
                    {
                        i_caracter = Leer.Read();
                        if (i_caracter == '/') finComentario = true;
                    }
                }

                if (!finComentario) Error(i_caracter);
                return true;
            }

            return false;
        }

        private void VerificarDirectivaInclude()
        {
            if (archivo == null) return;

            int lineaNum = 1;
            Regex patronInclude = new Regex(@"^\s*#\s*include\s*(<[^>]+>|""[^""]+"")\s*$");
            Regex patronDirective = new Regex(@"^\s*#\s*(\w+)", RegexOptions.Compiled);

            using (StreamReader sr = new StreamReader(archivo))
            {
                string linea;
                while ((linea = sr.ReadLine()) != null)
                {
                    string lineaTrimStart = linea.TrimStart();

                    if (lineaTrimStart.StartsWith("#"))
                    {
                        Match m = patronDirective.Match(lineaTrimStart);
                        if (m.Success)
                        {
                            string directiveName = m.Groups[1].Value; 

                            if (directiveName == "include")
                            {
                                if (!patronInclude.IsMatch(lineaTrimStart))
                                {
                                    Rtbx_salida.AppendText($"Error en directiva include en línea {lineaNum}: {linea}\n");
                                    N_error++;
                                }
                            }
                            else
                            {
                                Rtbx_salida.AppendText($"Directiva desconocida o mal escrita en línea {lineaNum}: {linea}\n");
                                N_error++;
                            }
                        }
                        else
                        {
                            Rtbx_salida.AppendText($"Error en directiva en línea {lineaNum}: {linea}\n");
                            N_error++;
                        }
                    }

                    lineaNum++;
                }
            }
        }

        private void Declaracion(string linea, int numLinea)
        {
            string[] tokens = linea.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length == 0) return;

            string tipo = tokens[0];

            if (tipo.StartsWith("#"))
                return;

            if (tipo == "int" || tipo == "float" || tipo == "double" || tipo == "char")
            {
                if (linea.Contains("["))
                {
                    Declaracion_Arreglo(linea, numLinea);
                }
                else
                {
                    Declaracion_VariableGlobal(linea, numLinea);
                }
            }
            else
            {
                Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: tipo de dato desconocido '{tipo}'\n");
                N_error++;
            }
        }


        private void Declaracion_VariableGlobal(string linea, int numLinea)
        {
            string patron = @"^(int|float|double|char)\s+[a-zA-Z_]\w*\s*(=\s*[-+]?[0-9]*\.?[0-9]+)?\s*;\s*$";

            if (!Regex.IsMatch(linea, patron))
            {
                if (!linea.TrimEnd().EndsWith(";"))
                {
                    Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: falta ';' al final");
                }
                else
                {
                    Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: {linea}\n");
                }

                N_error++;
            }
        }

        private bool Constante(string valor)
        {
            return int.TryParse(valor, out int resultado) && resultado > 0;
        }

        private void Declaracion_Arreglo(string linea, int numLinea)
        {
            Match match = Regex.Match(linea,
                @"^(int|float|double|char)\s+[a-zA-Z_]\w*\s*(\[\s*\d+\s*\])+\s*=\s*\{.*\}\s*;\s*$");

            if (match.Success)
            {
                string contenido = linea.Substring(linea.IndexOf('=') + 1);

                int abre = contenido.Count(c => c == '{');
                int cierra = contenido.Count(c => c == '}');
                if (abre != cierra)
                {
                    Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: llaves faltantes - {linea}\n");
                    N_error++;
                    return;
                }

                if (Regex.IsMatch(contenido, @"\}\s*\{"))
                {
                    Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: falta coma entre grupos - {linea}\n");
                    N_error++;
                    return;
                }

                string[] grupos = Regex.Split(contenido, @"\},\s*\{");
                foreach (string grupo in grupos)
                {
                    string limpio = grupo.Replace("{", "").Replace("}", "").Trim();

                    if (Regex.IsMatch(limpio, @"\d+\s+\d+"))
                    {
                        Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: falta coma entre valores - {linea}\n");
                        N_error++;
                        return;
                    }

                    string[] elementos = limpio.Split(',');
                    if (elementos.Any(e => string.IsNullOrWhiteSpace(e)))
                    {
                        Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: coma faltante o valor vacío {linea}\n");
                        N_error++;
                        return;
                    }
                }

                if (!linea.TrimEnd().EndsWith(";"))
                {
                    Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: falta ';' al final - {linea}\n");
                    N_error++;
                }
            }
            else
            {
                Rtbx_salida.AppendText($"Error sintáctico en línea {numLinea}: formato inválido de arreglo → {linea}\n");
                N_error++;
            }
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
                    string trimmed = linea.TrimStart();
                    if (string.IsNullOrWhiteSpace(trimmed) ||
                        trimmed.StartsWith("//") ||
                        trimmed.StartsWith("/*") ||
                        trimmed.StartsWith("*") ||
                        trimmed.StartsWith("*/"))
                    {
                        numLinea++;
                        continue;
                    }

                    Declaracion(linea.Trim(), numLinea);
                    numLinea++;
                }
            }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
        }

        private void traducirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                string codigoTraducido = TraducirCodigo(richTextBox1.Text);
               

                if (archivo != null)
                {
                    string archivoTrad = archivo.Remove(archivo.Length - 1) + "trad";
                    using (StreamWriter EscribirTrad = new StreamWriter(archivoTrad))
                    {
                        EscribirTrad.Write(codigoTraducido);
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

        private void AnalisisLexico()
        {
            elemento = "";
            Numero_linea = 1;
            int erroresLexicos = 0;

            archivoback = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoback);
            Leer = new StreamReader(archivo);

            Regex tokenRegex = new Regex(@"#|include|<[^>]+>|""[^""]+""|[a-zA-Z_]\w*|\d+|[{}()\[\];,+\-*/=%<>]|//.*|/\*.*?\*/",
                RegexOptions.Compiled | RegexOptions.Singleline);

            Regex simbolosInvalidos = new Regex(@"[^a-zA-Z0-9_{}\[\]\(\);\#\""<>\+\-\*/=%\s,\.]");

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

                    if (trimmed.StartsWith("#include"))
                    {
                        if (!includeValido.IsMatch(trimmed))
                        {
                            Rtbx_salida.AppendText($"Error léxico en línea {Numero_linea}: librería mal escrita - {linea}\n");
                            erroresLexicos++;
                        }

                        linea = Regex.Replace(linea, @"<[^>]+>|""[^""]+""", "libreria");
                    }

                    MatchCollection tokens = tokenRegex.Matches(linea);
                    foreach (Match token in tokens)
                    {
                        string valor = token.Value.Trim();

                        if (string.IsNullOrWhiteSpace(valor))
                            continue;

                        Escribir.WriteLine(valor);
                    }

                    if (!trimmed.StartsWith("//") && !trimmed.Contains("/*"))
                    {
                        Match match = simbolosInvalidos.Match(linea);
                        if (match.Success)
                        {
                            Rtbx_salida.AppendText($"Error léxico en línea {Numero_linea}: carácter no válido '{match.Value}'\n");
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


        private void analizarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rtbx_salida.Clear();
            N_error = 0;
            guardar();

            AnalisisLexico();

            VerificarDirectivaInclude();
            VerificarDeclaraciones();

            Rtbx_salida.AppendText($"\nTotal de errores sintácticos y léxicos: {N_error}\n");
        }
    }
    
}
