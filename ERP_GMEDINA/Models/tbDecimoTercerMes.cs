
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
    
public partial class tbDecimoTercerMes
{

    public int dtm_IdDecimoTercerMes { get; set; }

    public System.DateTime dtm_FechaPago { get; set; }

    public int dtm_UsuarioCrea { get; set; }

    public System.DateTime dtm_FechaCrea { get; set; }

    public Nullable<int> dtm_UsuarioModifica { get; set; }

    public Nullable<System.DateTime> dtm_FechaModifica { get; set; }

    public Nullable<int> emp_Id { get; set; }

    public Nullable<decimal> dtm_Monto { get; set; }

    public string dtm_CodigoPago { get; set; }



    public virtual tbUsuario tbUsuario { get; set; }

    public virtual tbUsuario tbUsuario1 { get; set; }

    public virtual tbEmpleados tbEmpleados { get; set; }

}

}
