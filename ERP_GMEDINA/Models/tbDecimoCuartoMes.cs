
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
    
public partial class tbDecimoCuartoMes
{

    public int dcm_IdDecimoCuartoMes { get; set; }

    public System.DateTime dcm_FechaPago { get; set; }

    public int dcm_UsuarioCrea { get; set; }

    public System.DateTime dcm_FechaCrea { get; set; }

    public Nullable<int> dcm_UsuarioModifica { get; set; }

    public Nullable<System.DateTime> dcm_FechaModifica { get; set; }

    public int emp_Id { get; set; }

    public Nullable<decimal> dcm_Monto { get; set; }

    public string dcm_CodigoPago { get; set; }



    public virtual tbUsuario tbUsuario { get; set; }

    public virtual tbUsuario tbUsuario1 { get; set; }

    public virtual tbEmpleados tbEmpleados { get; set; }

}

}
