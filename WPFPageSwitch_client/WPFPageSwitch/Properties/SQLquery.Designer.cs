﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace WPFPageSwitch.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
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
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("WPFPageSwitch.Properties.SQLquery", typeof(SQLquery).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
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
        ///   Looks up a localized string similar to create table file (id INTEGER PRIMARY KEY ASC, 
        ///nome_file_c varchar(50), 
        ///path_relativo_c varchar(100), 
        ///t_creazione timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, 
        ///valido BOOLEAN DEFAULT TRUE);.
        /// </summary>
        internal static string tabellaFile {
            get {
                return ResourceManager.GetString("tabellaFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to create table snapshots (id INTEGER PRIMARY KEY ASC, 
        ///                        id_file int,
        ///                        dim int, 
        ///                        t_modifica timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP, 
        ///                        valido BOOLEAN DEFAULT TRUE,
        ///                        nome_locale_s varchar(100), 
        ///                        sha_contenuto char(128), 
        ///                        FOREIGN KEY (id_file) REFERENCES file(id) on delete cascade);.
        /// </summary>
        internal static string tabellaSnapshot {
            get {
                return ResourceManager.GetString("tabellaSnapshot", resourceCulture);
            }
        }
    }
}
