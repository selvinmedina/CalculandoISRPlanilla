
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
    
public partial class tbHistorialVacaciones
{

    public int hvac_Id { get; set; }

    public int emp_Id { get; set; }

    public System.DateTime hvac_FechaInicio { get; set; }

    public System.DateTime hvac_FechaFin { get; set; }

    public Nullable<int> hvac_CantDias { get; set; }

    public bool hvac_DiasPagados { get; set; }

    public int hvac_MesVacaciones { get; set; }

    public int hvac_AnioVacaciones { get; set; }

    public bool hvac_Estado { get; set; }

    public string hvac_RazonInactivo { get; set; }

    public int hvac_UsuarioCrea { get; set; }

    public System.DateTime hvac_FechaCrea { get; set; }

    public Nullable<int> hvac_UsuarioModifica { get; set; }

    public Nullable<System.DateTime> hvac_FechaModifica { get; set; }



    public virtual tbUsuario tbUsuario { get; set; }

    public virtual tbUsuario tbUsuario1 { get; set; }

    public virtual tbEmpleados tbEmpleados { get; set; }

}

}
