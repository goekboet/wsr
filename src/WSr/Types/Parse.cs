using System;

namespace WSr
{
    public class Parse<TError, TParse>
    {
        private TParse Data;
        private TError Error;


        public Parse(TError e)
        {
            Error = e;
        }

        public Parse(TParse p)
        {
            Data = p;
        }

        public void Deconstruct(out TError e, out TParse p)
        {
            e = Error;
            p = Data;   
        }

        public bool IsError => Error != null;

        public Parse<TError, TParse> Map(Func<TParse, Parse<TError, TParse>> f) => IsError 
            ? this 
            : f(Data);

        public override string ToString() => IsError 
            ? $"Error: {Error.ToString()}"
            : $"Parse: {Data.ToString()}";

        public override bool Equals(object obj)
        {
            if (obj is Parse<TError, TParse> p)
            {
                (var e, var d) = p;

                return p.IsError ? e.Equals(Error) : d.Equals(Data);
            }
            else return false;
        }

        public override int GetHashCode() => IsError ? Error.GetHashCode() : Data.GetHashCode(); 
          
            
    }
}