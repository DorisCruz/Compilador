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

        public Form1()
        {
            InitializeComponent();
            compilarSolucionToolStripMenuItem.Enabled = false;

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
            compilarSolucionToolStripMenuItem.Enabled = true;
            //habilita la opcion compilar cuando se carga un archivo.
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
            if (caracter >= 65 && caracter <= 90 || caracter >= 97 && caracter <= 122) { return 'l'; } //letra 
            else
            {
                if (caracter >= 48 && caracter <= 57) { return 'd'; } //digito 
                else
                {
                    switch (caracter)
                    {
                        case 10: return 'n'; //salto de linea
                        case 34: return '"';//inicio de cadena
                        case 39: return 'c';//inicio de caracter
                        case 32: return 'e';//espacio

                        //programar para los casos que sean simbolos para regresar 's'

                        default: return 's';//simbolo
                    }
                    ;

                }
            }

        }

        private void Simbolo()
        {
            if (i_caracter == 33 ||
                i_caracter >= 35 && i_caracter <= 38 ||
                i_caracter >= 40 && i_caracter <= 45 ||
                i_caracter == 47 ||
                i_caracter >= 58 && i_caracter <= 62 ||
                i_caracter == 91 ||
                i_caracter == 93 ||
                i_caracter == 94 ||
                i_caracter == 123 ||
                i_caracter == 124 ||
            i_caracter == 125
            ) { elemento = elemento + (char)i_caracter + "\n"; } //simbolos validos 
            else { Error(i_caracter); }
        }
        private void Cadena()
        {
            do
            {
                i_caracter = Leer.Read();
                if (i_caracter == 10) Numero_linea++;

            } while (i_caracter != 34 && i_caracter != -1);
            if (i_caracter == -1) Error(-1);
        }

        private void Caracter()
        {
            i_caracter = Leer.Read();
            //programar para los casos donde el caracter se imprime  '\n','\r' etc.
            i_caracter = Leer.Read();
            if (i_caracter != 39) Error(39);
        }
        private void Error(int i_caracter)
        {
            Rtbx_salida.AppendText("Error léxico " + (char)i_caracter + ", línea " + Numero_linea + "\n");
            N_error++;
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
            do
            {
                elemento = elemento + (char)i_caracter;
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'l' || Tipo_caracter(i_caracter) == 'd');
            if ((char)i_caracter == '.') { Archivo_Libreria(); }
            else
            {
                if (Palabra_Reservada()) elemento = "Palabra Reservada\n";
                else elemento = "identificador\n";
            }

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
            do
            {
                i_caracter = Leer.Read();
            } while (Tipo_caracter(i_caracter) == 'd');
            if ((char)i_caracter == '.') { Numero_Real(); }
            else
            {
                elemento = "numero_entero\n";
            }

        }

        private void compilarSolucionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Rtbx_salida.Text = "";
            guardar();
            elemento = "";
            N_error = 0;
            Numero_linea = 1;

            int contadorIdentificadores = 0;
            int contadorReservadas = 0;
            int contadorNumeros = 0;
            int contadorSimbolos = 0;
            int contadorCadenas = 0;
            int contadorCaracteres = 0;

            archivoback = archivo.Remove(archivo.Length - 1) + "back";
            Escribir = new StreamWriter(archivoback);
            Leer = new StreamReader(archivo);

            i_caracter = Leer.Read();
            do
            {
                elemento = "";
                switch (Tipo_caracter(i_caracter))
                {
                    case 'l':
                        Identificador();
                        Escribir.Write(elemento);
                        Rtbx_salida.AppendText(elemento);

                        if (elemento.Contains("Palabra Reservada"))
                            contadorReservadas++;
                        else
                            contadorIdentificadores++;
                        break;

                    case 'd':
                        Numero();
                        Escribir.Write(elemento);
                        Rtbx_salida.AppendText(elemento);
                        contadorNumeros++;
                        break;

                    case 's':
                        Simbolo();
                        Escribir.Write(elemento);
                        Rtbx_salida.AppendText(elemento);
                        contadorSimbolos++;
                        i_caracter = Leer.Read();
                        break;

                    case '"':
                        Cadena();
                        Escribir.Write("cadena\n");
                        Rtbx_salida.AppendText("cadena\n");
                        contadorCadenas++;
                        i_caracter = Leer.Read();
                        break;

                    case 'c':
                        Caracter();
                        Escribir.Write("caracter\n");
                        Rtbx_salida.AppendText("caracter\n");
                        contadorCaracteres++;
                        i_caracter = Leer.Read();
                        break;

                    case 'n':
                        i_caracter = Leer.Read();
                        Numero_linea++;
                        break;

                    case 'e':
                        i_caracter = Leer.Read();
                        break;

                    default:
                        Error(i_caracter);
                        break;
                }

            } while (i_caracter != -1);

            Rtbx_salida.AppendText("Palabras reservadas: " + contadorReservadas + "\n");
            Rtbx_salida.AppendText("Identificadores: " + contadorIdentificadores + "\n");
            Rtbx_salida.AppendText("Números: " + contadorNumeros + "\n");
            Rtbx_salida.AppendText("Símbolos: " + contadorSimbolos + "\n");
            Rtbx_salida.AppendText("Cadenas: " + contadorCadenas + "\n");
            Rtbx_salida.AppendText("Caracteres: " + contadorCaracteres + "\n");
            Rtbx_salida.AppendText("Errores: " + N_error + "\n");

            Escribir.Close();
            Leer.Close();
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            compilarSolucionToolStripMenuItem.Enabled = true;
            //habilita la opcion compilar cuando se realiza un cambio en el texto.
        }
    }
    
}
