﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Il codice è stato generato da uno strumento.
//     Versione runtime:4.0.30319.42000
//
//     Le modifiche apportate a questo file possono provocare un comportamento non corretto e andranno perse se
//     il codice viene rigenerato.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ProgettoMalnati.Properties {
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
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("ProgettoMalnati.Properties.SQLquery", typeof(SQLquery).Assembly);
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
        ///   Cerca una stringa localizzata simile a UPDATE utenti SET nome = @nuovo_nome WHERE nome = @nome;.
        /// </summary>
        internal static string sqlAggiornaNomeUtente {
            get {
                return ResourceManager.GetString("sqlAggiornaNomeUtente", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a UPDATE utenti SET password = @pass WHERE nome =@nome;.
        /// </summary>
        internal static string sqlAggiornaPassword {
            get {
                return ResourceManager.GetString("sqlAggiornaPassword", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a UPDATE fileutente SET nome_file_c = @nome_file_c WHERE id = @id;.
        /// </summary>
        internal static string sqlCambiaNomeFile {
            get {
                return ResourceManager.GetString("sqlCambiaNomeFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a UPDATE fileutente SET path_relativo_c = @path_relativo_c WHERE id=@id;.
        /// </summary>
        internal static string sqlCambiaPathFile {
            get {
                return ResourceManager.GetString("sqlCambiaPathFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT id FROM fileutente WHERE valido = false AND nome_utente = @nome_utente AND t_creazione = ( SELECT MIN(t_creazione) FROM fileutente WHERE valido = false AND nome_utente = @nome_utente);.
        /// </summary>
        internal static string sqlCercaFileDaDistruggere {
            get {
                return ResourceManager.GetString("sqlCercaFileDaDistruggere", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT * FROM utenti WHERE nome = @nome AND password = @password;.
        /// </summary>
        internal static string sqlCheckUtente {
            get {
                return ResourceManager.GetString("sqlCheckUtente", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT count(*) as conteggio FROM utenti WHERE nome = @nome;.
        /// </summary>
        internal static string sqlControllaNomeUtente {
            get {
                return ResourceManager.GetString("sqlControllaNomeUtente", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT nome_file_c, path_relativo_c, t_creazione, valido FROM fileutente WHERE id = @id AND nome_utente = @nome_utente;.
        /// </summary>
        internal static string sqlGetFileData {
            get {
                return ResourceManager.GetString("sqlGetFileData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT DISTINCT nome_file_c FROM fileutente WHERE nome_utente = @nome_utente AND path_relativo_c = @path_relativo_c;.
        /// </summary>
        internal static string sqlGetFileNames {
            get {
                return ResourceManager.GetString("sqlGetFileNames", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT id FROM fileutente WHERE nome_utente = @nome_utente ORDER BY t_creazione DESC;.
        /// </summary>
        internal static string sqlGetIdsFiles {
            get {
                return ResourceManager.GetString("sqlGetIdsFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT id FROM snapshots WHERE id_file = @id_file ORDER BY t_modifica DESC;.
        /// </summary>
        internal static string sqlGetIdsSnapshots {
            get {
                return ResourceManager.GetString("sqlGetIdsSnapshots", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT DISTINCT path_relativo_c FROM fileutente WHERE nome_utente = @nome_utente;.
        /// </summary>
        internal static string sqlGetPathNames {
            get {
                return ResourceManager.GetString("sqlGetPathNames", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT dim, t_modifica, sha_contenuto, nome_locale_s, valido FROM snapshots WHERE id = @id;.
        /// </summary>
        internal static string sqlGetSnapshotData {
            get {
                return ResourceManager.GetString("sqlGetSnapshotData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a SELECT t_modifica FROM snapshots WHERE id_file = @id_file;.
        /// </summary>
        internal static string sqlGetVersions {
            get {
                return ResourceManager.GetString("sqlGetVersions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a INSERT INTO snapshots (id_file, dim, t_modifica, sha_contenuto, nome_locale_s) VALUES (@id_file, @dim, @t_modifica, @sha_contenuto, @nome_locale_s);.
        /// </summary>
        internal static string sqlInsertSnapshotData {
            get {
                return ResourceManager.GetString("sqlInsertSnapshotData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a INSERT INTO fileutente(nome_utente, nome_file_c, path_relativo_c, t_creazione) VALUES (@nome_utente, @nome_file_c, @path_relativo_c,  @t_creazione).
        /// </summary>
        internal static string sqlNuovoFile {
            get {
                return ResourceManager.GetString("sqlNuovoFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a INSERT INTO utenti(nome,password) VALUES (@nome,@password);.
        /// </summary>
        internal static string sqlSetInfoUtente {
            get {
                return ResourceManager.GetString("sqlSetInfoUtente", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a UPDATE snapshots SET dim = @dim, t_modifica = @t_modifica, sha_contenuto = @sha_contenuto, nome_locale_s = @nome_locale_s WHERE id = @id;.
        /// </summary>
        internal static string sqlStoreSnapshotData {
            get {
                return ResourceManager.GetString("sqlStoreSnapshotData", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a create table fileutente (nome_utente varchar(20), id INTEGER PRIMARY KEY ASC, nome_file_c varchar(50), path_relativo_c varchar(100), t_creazione timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, valido BOOLEAN DEFAULT TRUE, FOREIGN KEY (nome_utente) REFERENCES utenti(nome) ON DELETE CASCADE ON UPDATE CASCADE); .
        /// </summary>
        internal static string tabellaFileUtente {
            get {
                return ResourceManager.GetString("tabellaFileUtente", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a create table snapshots (
        ///                        id INTEGER PRIMARY KEY ASC, 
        ///                        id_file int,
        ///                        dim int, 
        ///                        t_modifica timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, 
        ///                        valido BOOLEAN DEFAULT TRUE,
        ///                        nome_locale_s varchar(100), 
        ///                        sha_contenuto char(128), 
        ///                        FOREIGN KEY (id_file) REFERENCES fileutente(id) on delete cascade);.
        /// </summary>
        internal static string tabellaSnapshot {
            get {
                return ResourceManager.GetString("tabellaSnapshot", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Cerca una stringa localizzata simile a create table utenti (nome varchar(20), password varchar(100), PRIMARY KEY(nome)).
        /// </summary>
        internal static string tabellaUtenti {
            get {
                return ResourceManager.GetString("tabellaUtenti", resourceCulture);
            }
        }
    }
}
