using Compilador_AAA.Traductor;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics.Metrics;
using System.Collections.ObjectModel;
using Compilador_AAA.Models;
using ICSharpCode.AvalonEdit.Highlighting;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Compilador_AAA.Views
{
    /// <summary>
    /// Lógica de interacción para TranslatorView.xaml
    /// </summary>
    public partial class TranslatorView : UserControl
    {
        public TranslatorView()
        {
            ;
            InitializeComponent();
            Loaded += TranslatorView_Loaded;
            lvErrores.ItemsSource = ErrorList;
        }
        public static string _translation { get; set; }
        public static List<string> _print = new List<string>();
        private void TranslatorView_Loaded(object sender, RoutedEventArgs e)
        {
            OriginalEditor.Focus();

        }

        public static ObservableCollection<ErrorRow> ErrorList { get; set; } = new ObservableCollection<ErrorRow>();

        // Método para manejar errores
        public static void HandleError(string errorMessage, int position, string code)
        {
            var errorRow = new ErrorRow
            {
                emp1 = "",
                Code = "", // Código de error
                Description = errorMessage,
                Line = position.ToString(),
                Image = "", // Ruta de la imagen, si es necesario
                emp2 = "",
            };

            ErrorList.Add(errorRow);
        }

        private bool _isDragging = false;
        private Point _startPoint;
        private void btnTraducir_Click(object sender, RoutedEventArgs e)
        {
            _translation = "";
            if (RTErrorList)
            {
                if (OriginalEditor.Text != string.Empty)
                {
                    _print.Clear();
                    ErrorList.Clear();
                    TranslatedEditor.Text = string.Empty;

                    try
                    {
                        // Tokenizar el texto original
                        Lexer lexer = new Lexer(OriginalEditor.Document);
                        var tokensTuple = lexer.Tokenize();
                        // Parsear los tokens
                        Parser parser = new Parser(tokensTuple);
                        var program = parser.Parse(); // Asegúrate de que el método Parse() devuelva un objeto Program

                        // Realizar análisis semántico
                        var semanticAnalyzer = new SemanticAnalyzer();
                        program.Accept(semanticAnalyzer);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
            }
            if (ErrorList.Count == 0)
            {
                TranslatedEditor.Text = _translation;
                if (_print.Count != 0)
                {
                    // Concatenar todos los elementos de la lista _print en un solo string
                    string concatenatedString = string.Join("\n", _print); // Usar "\n" como delimitador

                    // Mostrar el resultado en un MessageBox
                    MessageBox.Show(concatenatedString,"Consola");
                }
                

            }
            else
            {
                TranslatedEditor.Text = "";
            }
            
            

        }

        private void lvErrores_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var gridView = lvErrores.View as GridView;

            if (gridView != null)
            {
                // Ancho total disponible
                double totalWidth = lvErrores.ActualWidth - SystemParameters.VerticalScrollBarWidth;

                // Proporciones de las columnas
                gridView.Columns[0].Width = 25;
                gridView.Columns[1].Width = 25;
                gridView.Columns[2].Width = 55;
                if (totalWidth - 185 > 80)
                    gridView.Columns[3].Width = totalWidth - 185;
                gridView.Columns[4].Width = 45;
                gridView.Columns[5].Width = 25;
            }
        }

        private void OriginalEditor_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void OriginalEditor_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void TranslatedEditor_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void TranslatedEditor_LostFocus(object sender, RoutedEventArgs e)
        {
        }

        private void ErrorsWindow_GotFocus(object sender, RoutedEventArgs e)
        {
        }

        private void ErrorsWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            errorlistheadercolor.Background = new SolidColorBrush(Colors.Transparent);
        }

        private void OriginalEditor_TextChanged(object sender, EventArgs e)
        {
            
        }
        private bool RTErrorList = true;
        private void btnDebug_Click(object sender, RoutedEventArgs e)
        {
            if (RTErrorList)
            {
                btnDebug.Content = "RT:OFF";
                RTErrorList = false;
            }
            else
            {
                RTErrorList = true;
                btnDebug.Content = "RT:ON";
            }
        }
    }
}

