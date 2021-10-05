using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnilistESP
{

    class ServiciosSingleton
    {
        private static ServiciosSingleton _instance;
        private bool _yepMode;

        private static object syncLock = new object();

        private ServiciosSingleton()
        {
            _yepMode = false;
        }

        public static ServiciosSingleton GetServiciosSingleton()
        {
            if (_instance == null)
            {
                lock (syncLock)
                {
                    if (_instance == null)
                    {
                        _instance = new ServiciosSingleton();
                    }
                }
            }

            return _instance;
        }

        public bool YepMode
        {
            get
            {
                return _yepMode;
            }
        }

        public void CambiarYepMode()
        {
            if (_yepMode)
            {
                _yepMode = false;
            }
            else
            {
                _yepMode = true;
            }
        }

    }
}
