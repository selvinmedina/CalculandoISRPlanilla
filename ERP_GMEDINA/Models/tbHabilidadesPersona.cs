
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------


namespace ERP_GMEDINA.Models
{

using System;
    using System.Collections.Generic;
    
public partial class tbHabilidadesPersona
{

    public int hape_Id { get; set; }

    public int per_Id { get; set; }

    public int habi_Id { get; set; }

    public bool hape_Estado { get; set; }

    public string hape_RazonInactivo { get; set; }

    public int hape_UsuarioCrea { get; set; }

    public System.DateTime hape_FechaCrea { get; set; }

    public Nullable<int> hape_UsuarioModifica { get; set; }

    public Nullable<System.DateTime> hape_FechaModifica { get; set; }



    public virtual tbUsuario tbUsuario { get; set; }

    public virtual tbUsuario tbUsuario1 { get; set; }

    public virtual tbHabilidades tbHabilidades { get; set; }

    public virtual tbPersonas tbPersonas { get; set; }

}

}
