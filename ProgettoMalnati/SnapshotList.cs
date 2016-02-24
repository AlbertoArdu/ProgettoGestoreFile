using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgettoMalnati
{
    class SnapshotList: IEnumerable<Snapshot>
    {
        //Attributi
        private string __nome_utente;
        private int[] __list_ids_files;
        //Proprieta
        public string NomeUtente
        {
            get{ return __nome_utente; }
        }
        public ProgettoMalnati.Snapshot this[int index] 
        {
            get 
            {
                ProgettoMalnati.Snapshot ss = new Snapshot(__nome_utente, __list_ids_files[index]);
                return ss;
            }
            set { }
        }
        //Costruttori
        public SnapshotList(string nome_utente)
        {
            //Leggere gli id dei file di questo utente e metterli in __list_ids_files

        }
        //Distruttore
        //Metodi
        public IEnumerator<Snapshot> GetEnumerator() 
        {
            int index;
            for (index = 0; index < this.__list_ids_files.Length; index++)
            {
                yield return new Snapshot(this.__nome_utente, __list_ids_files[index]);
            }
        }
        //Metodi Statici
    }
}
