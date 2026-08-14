using System;
using System.Collections.Generic;
using System.Text;

namespace ProyectoEcommerce.Utilidades
{
    // todas las capas usan esta caja para devolver datos o explicar que salio mal
    // T cambia segun lo que devuelve cada metodo, por ejemplo Usuario, bool o una lista
    public partial class Respuesta<T>
    {

        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Error { get; set; }


        public Respuesta()
        {
            // una respuesta empieza correcta y solo cambia cuando alguna capa encuentra un error
            Success = true;
            Error = "";
        }


        protected Respuesta(bool success, T data, string error)
        {
            Success = success;
            Data = data;    
            Error = error;
        }

        // estos dos atajos crean una respuesta lista sin repetir las propiedades cada vez
        public static Respuesta<T> Ok(T data) => new(true,  data, null);

        public static Respuesta<T> Fail(String error) => new(false, default, error);

    }
}

