﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Il codice è stato generato da uno strumento.
//     Versione runtime:4.0.30319.42000
//
//     Le modifiche apportate a questo file possono provocare un comportamento non corretto e andranno perse se
//     il codice viene rigenerato.
// </auto-generated>
//------------------------------------------------------------------------------

namespace clientWPF.Properties {
    using System;
    
    
    /// <summary>
    ///   Classe di risorse fortemente tipizzata per la ricerca di stringhe localizzate e così via.
    /// </summary>
    // Questa classe è stata generata automaticamente dalla classe StronglyTypedResourceBuilder.
    // tramite uno strumento quale ResGen o Visual Studio.
    // Per aggiungere o rimuovere un membro, modificare il file con estensione ResX ed eseguire nuovamente ResGen
    // con l'opzione /str oppure ricompilare il progetto VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SQLquery {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SQLquery() {
        }
        
        /// <summary>
        ///   Restituisce l'istanza di ResourceManager nella cache utilizzata da questa classe.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("clientWPF.Properties.SQLquery", typeof(SQLquery).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Esegue l'override della proprietà CurrentUICulture del thread corrente per tutte le
        ///   ricerche di risorse eseguite utilizzando questa classe di risorse fortemente tipizzata.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a insert into Versioni(id_file,timestap_vers) VALUES (@id_file, @timestamp_vers);.
        /// </summary>
        internal static string sqlAddVersion {
            get {
                return ResourceManager.GetString("sqlAddVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT id FROM file WHERE valido = &apos;FALSE&apos;;.
        /// </summary>
        internal static string sqlGetDeletedIds {
            get {
                return ResourceManager.GetString("sqlGetDeletedIds", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT nome_file_c, path_relativo_c, t_creazione,  t_modifica, sha_contenuto, dim, valido FROM file WHERE id = @id_file;.
        /// </summary>
        internal static string sqlGetFileData {
            get {
                return ResourceManager.GetString("sqlGetFileData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT id FROM file WHERE valido = &apos;TRUE&apos;;.
        /// </summary>
        internal static string sqlGetId {
            get {
                return ResourceManager.GetString("sqlGetId", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT timestamp_vers FROM versioni WHERE id_file = @id_file ;.
        /// </summary>
        internal static string sqlGetVersionData {
            get {
                return ResourceManager.GetString("sqlGetVersionData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a INSERT INTO file (nome_file_c, path_relativo_c, dim, t_modifica, t_creazione, sha_contenuto) VALUES(@nome_file, @path, @dim, @t_modifica, @t_creazione, @sha_contenuto);.
        /// </summary>
        internal static string sqlNuovoFile {
            get {
                return ResourceManager.GetString("sqlNuovoFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a UPDATE file SET dim = @dim, t_modifica = @t_modifica, sha_contenuto = @sha_contenuto, valido = @valido WHERE id = @id;.
        /// </summary>
        internal static string sqlSetFileData {
            get {
                return ResourceManager.GetString("sqlSetFileData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a create table file (
        ///id INTEGER PRIMARY KEY ASC, 
        ///nome_file_c varchar(50), 
        ///path_relativo_c varchar(100),
        ///dim int, 
        ///t_modifica datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, t_creazione datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
        ///sha_contenuto char(128), valido BOOLEAN DEFAULT TRUE);.
        /// </summary>
        internal static string tabellaFile {
            get {
                return ResourceManager.GetString("tabellaFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a create table versioni (id_file integer, timestamp_vers datetime NOT NULL DEFAULT CURRENT_TIMESTAMP, PRIMARY KEY (id_file, timestamp_vers), FOREIGN KEY (id_file) REFERENCES file(id) on delete cascade);.
        /// </summary>
        internal static string TabellaVersioni {
            get {
                return ResourceManager.GetString("TabellaVersioni", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a CREATE TRIGGER max_versioni AFTER INSERT ON Versioni FOR EACH ROW BEGIN DELETE FROM Versioni WHERE timestamp_vers = (SELECT MIN(timestamp_vers) FROM Versioni	WHERE id_file = NEW.id_file )	AND id_file IN (SELECT id_file FROM Versioni WHERE id_file = NEW.id_file GROUP BY id_file HAVING COUNT(*) &gt; 3);.
        /// </summary>
        internal static string triggerNumeroVersioni {
            get {
                return ResourceManager.GetString("triggerNumeroVersioni", resourceCulture);
            }
        }
    }
}
