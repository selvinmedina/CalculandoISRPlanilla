
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
    
public partial class tbCatalogoDeIngresos
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public tbCatalogoDeIngresos()
    {

        this.tbEmpleadoBonos = new HashSet<tbEmpleadoBonos>();

        this.tbEmpleadoComisiones = new HashSet<tbEmpleadoComisiones>();

        this.tbTipoPlanillaDetalleIngreso = new HashSet<tbTipoPlanillaDetalleIngreso>();

        this.tbHistorialDeIngresosPago = new HashSet<tbHistorialDeIngresosPago>();

    }


    public int cin_IdIngreso { get; set; }

    public string cin_DescripcionIngreso { get; set; }

    public int cin_UsuarioCrea { get; set; }

    public System.DateTime cin_FechaCrea { get; set; }

    public Nullable<int> cin_UsuarioModifica { get; set; }

    public Nullable<System.DateTime> cin_FechaModifica { get; set; }

    public bool cin_Activo { get; set; }



    public virtual tbUsuario tbUsuario { get; set; }

    public virtual tbUsuario tbUsuario1 { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbEmpleadoBonos> tbEmpleadoBonos { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbEmpleadoComisiones> tbEmpleadoComisiones { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbTipoPlanillaDetalleIngreso> tbTipoPlanillaDetalleIngreso { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbHistorialDeIngresosPago> tbHistorialDeIngresosPago { get; set; }

}

}
