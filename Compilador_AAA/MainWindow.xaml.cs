using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Compilador_AAA.Views;
using Compilador_odalys.Views;

namespace Compilador_AAA
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TranslatorView translatorView = new TranslatorView();
            ContentPanel.Children.Add(translatorView);
            


        }





        private void Palabras_Reservadas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MsgView msg = new MsgView();
            msg.Show();
            msg.ChangeMessage("entero → int\ncadena → string\ndecimal → double\nbool → bool\npara → for\n si → if\nsino → else\nmientras → while\ndo → hacer\nimprime() → Console.WriteLine()");
        }

        private void TraductorMenu_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            

        }


        private void Window_Activated(object sender, EventArgs e)
        {
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
        }

        

        private void AcercaDe_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void Ejemplos_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MsgView msg = new MsgView();
            msg.Show();
            msg.ChangeMessage("//Operaciones aritmeticas jerarquicas\r\nentero num;\r\nnum = (5*4)/2+(25-18);\r\nimprime(num);\r\n\r\n//Contador Do While\r\nentero counter = 1;\r\nhacer\r\n{\r\n\timprime(counter);\r\n\tcounter = counter + 1;\r\n\t\r\n} mientras (counter < 10);\r\n\r\n//Contador While\r\nentero counter = 0;\r\nmientras (counter < 10)\r\n{\r\n\t\r\n\tcounter = counter + 1;\r\n\timprime(counter);\r\n}\r\n\r\n//Imprime el numero mayor If Else\r\nentero num1 = 5;\r\nentero num2 = 10;\r\nsi(num1 > num2){\r\n\timprime(num1);\r\n}sino{\r\n\timprime(num2);\r\n}\r\n\r\n//Factorial de un numero\r\nentero factorial = 1;\r\nentero num = 5;\r\npara(entero i = 1; i<=num; i = i + 1)\r\n{\r\n\tfactorial = factorial*i;\r\n\timprime(factorial);\r\n}\r\n");
        }
    }
}