
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
    
public partial class tbCatalogoDeDeducciones
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
    public tbCatalogoDeDeducciones()
    {

        this.tbDeduccionesExtraordinarias = new HashSet<tbDeduccionesExtraordinarias>();

        this.tbDeduccionInstitucionFinanciera = new HashSet<tbDeduccionInstitucionFinanciera>();

        this.tbTechosDeducciones = new HashSet<tbTechosDeducciones>();

        this.tbTipoPlanillaDetalleDeduccion = new HashSet<tbTipoPlanillaDetalleDeduccion>();

        this.tbHistorialDeduccionPago = new HashSet<tbHistorialDeduccionPago>();

    }


    public int cde_IdDeducciones { get; set; }

    public string cde_DescripcionDeduccion { get; set; }

    public int tde_IdTipoDedu { get; set; }

    public Nullable<decimal> cde_PorcentajeColaborador { get; set; }

    public Nullable<decimal> cde_PorcentajeEmpresa { get; set; }

    public int cde_UsuarioCrea { get; set; }

    public System.DateTime cde_FechaCrea { get; set; }

    public Nullable<int> cde_UsuarioModifica { get; set; }

    public Nullable<System.DateTime> cde_FechaModifica { get; set; }

    public bool cde_Activo { get; set; }



    public virtual tbUsuario tbUsuario { get; set; }

    public virtual tbUsuario tbUsuario1 { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbDeduccionesExtraordinarias> tbDeduccionesExtraordinarias { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbDeduccionInstitucionFinanciera> tbDeduccionInstitucionFinanciera { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbTechosDeducciones> tbTechosDeducciones { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbTipoPlanillaDetalleDeduccion> tbTipoPlanillaDetalleDeduccion { get; set; }

    public virtual tbTipoDeduccion tbTipoDeduccion { get; set; }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]

    public virtual ICollection<tbHistorialDeduccionPago> tbHistorialDeduccionPago { get; set; }

}

}
