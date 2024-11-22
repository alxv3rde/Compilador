using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador_AAA.Traductor
{
    public class Variable
    {
        public string VarType { get; set; } // Tipo de la variable (ej. "int", "double")
        public Expr? Value { get; set; } // Valor de la variable

        public Variable(string varType, Expr? value)
        {
            VarType = varType;
            Value = value;
        }
    }
}
