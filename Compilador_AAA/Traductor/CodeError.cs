using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador_AAA.Traductor
{
    public enum ErrorCode
    {
        LEX001, // Caracter inesperado
        LEX002, // Identificador no puede comenzar con un número
        LEX003, // Literal de cadena sin cerrar
        LEX004, // Literal de cadena sin cerrar
        LEX005, // Literal de cadena sin cerrar
        SYN001, // Paréntesis sin cerrar
        SYN002, // Token inesperado
        SYN003, // Fin de línea inesperado
        SYN004, // Fin de línea inesperado
        SYN005, // Fin de línea inesperado
        SEM001, // Variable no declarada
        SEM002, // Tipo incompatible
        SEM003,  // Acceso ilegal a miembro privado
        SEM004,  // Acceso ilegal a miembro privado
        SEM005  // Acceso ilegal a miembro privado
    }

    public class CodeError
    {
        public string Code { get; }
        public string Message { get; }
        public int Position { get; }
        public string Severity { get; } // "Warning" o "Error"

        public CodeError(string code, string message, int position, string severity = "Error")
        {
            Code = code;
            Message = message;
            Position = position;
            Severity = severity;
        }

        public override string ToString() => $"{Code} ({Severity}): {Message} (Posición {Position})";
    }
}
