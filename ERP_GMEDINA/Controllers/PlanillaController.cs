using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using ERP_GMEDINA.Models;
using SpreadsheetLight;
using DocumentFormat.OpenXml;
using ERP_GMEDINA.Helpers;

namespace ERP_GMEDINA.Controllers
{
    public class PlanillaController : Controller
    {
        /*PENDIENTE
        * CALCULAR ISR
        * Septimo día
        */
        private ERP_GMEDINAEntities db = new ERP_GMEDINAEntities();

        // GET: Planilla
        public ActionResult Index()
        {
            List<V_ColaboradoresPorPlanilla> colaboradoresPlanillas = db.V_ColaboradoresPorPlanilla.Where(x => x.CantidadColaboradores > 0).ToList();
            ViewBag.PlanillasColaboradores = colaboradoresPlanillas;
            ViewBag.colaboradoresGeneral = db.tbEmpleados.Count().ToString();
            return View(db.V_PreviewPlanilla.ToList());
        }

        public ActionResult GetPlanilla(int? ID)
        {
            List<V_PreviewPlanilla> PreviewPlanilla = new List<V_PreviewPlanilla>();

            if (ID != null)
                PreviewPlanilla = db.V_PreviewPlanilla.Where(x => x.cpla_IdPlanilla == ID).ToList();
            else
                PreviewPlanilla = db.V_PreviewPlanilla.ToList();
            return Json(PreviewPlanilla, JsonRequestBehavior.AllowGet);
        }

        //[HttpPost]
        public ActionResult GenerarPlanilla(int? ID, bool? enviarEmail, DateTime fechaInicio, DateTime fechaFin)
        {
            #region declaracion de instancias
            //helpers
            General utilities = new General();
            //para el voucher
            List<IngresosDeduccionesVoucher> ListaIngresosVoucher = new List<IngresosDeduccionesVoucher>();
            List<IngresosDeduccionesVoucher> ListaDeduccionesVoucher = new List<IngresosDeduccionesVoucher>();
            ComprobantePagoModel oComprobantePagoModel = new ComprobantePagoModel();
            IngresosDeduccionesVoucher ingresosColaborador = new IngresosDeduccionesVoucher();
            IngresosDeduccionesVoucher deduccionesColaborador = new IngresosDeduccionesVoucher();
            //para el reporte que se mandará a la vista
            ReportePlanillaViewModel oPlanillaEmpleado;
            List<ReportePlanillaViewModel> reporte = new List<ReportePlanillaViewModel>();

            //para enviar resultados al lado del cliente
            iziToast response = new iziToast();
            int errores = 0;
            #endregion          

            // INCIA PROCESO DE GENERACIÓN DE PLANILLAS
            try
            {

                #region CREAR ARCHIVO EXCEL DE LA PLANILLA
                tbCatalogoDePlanillas oNombrePlanilla = ID != null ? db.tbCatalogoDePlanillas.Where(X => X.cpla_IdPlanilla == ID).FirstOrDefault() : null;
                string nombrePlanilla = oNombrePlanilla != null ? oNombrePlanilla.cpla_DescripcionPlanilla : "General";
                string nombreDocumento = $"Planilla {nombrePlanilla} {Convert.ToString(DateTime.Now.Year)}-{Convert.ToString(DateTime.Now.Month)}-{Convert.ToString(DateTime.Now.Day)} {Convert.ToString(DateTime.Now.Hour)}-{Convert.ToString(DateTime.Now.Minute)}.xlsx";
                string nombreDocumento2 = nombreDocumento;
                string pathFile = AppDomain.CurrentDomain.BaseDirectory + nombreDocumento2;
                string direccion = pathFile;
                SLDocument oSLDocument = new SLDocument();
                System.Data.DataTable dt = new System.Data.DataTable();

                dt.Columns.Add("Nombres", typeof(string));
                dt.Columns.Add("Apellidos", typeof(string));
                dt.Columns.Add("Sueldo base", typeof(decimal));
                dt.Columns.Add("Bonos", typeof(decimal));
                dt.Columns.Add("Comisiones", typeof(decimal));
                dt.Columns.Add("Deducciones extras", typeof(decimal));
                dt.Columns.Add("Deducciones Cooperativas", typeof(decimal));
                dt.Columns.Add("IHSS", typeof(decimal));
                dt.Columns.Add("ISR", typeof(decimal));
                dt.Columns.Add("AFP", typeof(decimal));
                dt.Columns.Add("RAP", typeof(decimal));
                dt.Columns.Add("TOTAL A PAGAR", typeof(decimal));
                #endregion


                using (ERP_GMEDINAEntities db = new ERP_GMEDINAEntities())
                {
                    List<tbCatalogoDePlanillas> oIDSPlanillas = new List<tbCatalogoDePlanillas>();

                    //seleccionar las planillas que se van a generar
                    if (ID != null)
                        oIDSPlanillas = db.tbCatalogoDePlanillas.Where(X => X.cpla_IdPlanilla == ID).ToList();
                    else
                        oIDSPlanillas = db.tbCatalogoDePlanillas.Where(x => x.cpla_Activo == true).ToList();

                    //procesar todas las planillas seleccionadas
                    foreach (var iter in oIDSPlanillas)
                    {

                        try
                        {
                            //planilla actual del foreach
                            tbCatalogoDePlanillas oPlanilla = db.tbCatalogoDePlanillas.Find(iter.cpla_IdPlanilla);

                            //ingresos de la planilla actual
                            List<V_PlanillaIngresos> oIngresos = db.V_PlanillaIngresos.Where(x => x.cpla_IdPlanilla == oPlanilla.cpla_IdPlanilla).ToList();

                            //deducciones de la planilla actual
                            List<V_PlanillaDeducciones> oDeducciones = db.V_PlanillaDeducciones.Where(x => x.cpla_IdPlanilla == oPlanilla.cpla_IdPlanilla).ToList();

                            //empleados de la planilla actual
                            List<tbEmpleados> oEmpleados = db.tbEmpleados.Where(emp => emp.cpla_IdPlanilla == oPlanilla.cpla_IdPlanilla && emp.emp_Estado == true).ToList();

                            int contador = 1;
                            int idDetalleDeduccionHisotorialesContador = 1;
                            int idDetalleIngresoHisotorialesContador = 1;
                            //procesar planilla empleado por empleado
                            foreach (var empleadoActual in oEmpleados)
                            {
                                using (var dbContextTransaccion = db.Database.BeginTransaction())
                                {
                                    try
                                    {
                                        #region variables Reporte View Model

                                        string codColaborador = string.Empty;
                                        string nombreColaborador = string.Empty;
                                        decimal SalarioBase = 0;
                                        //Inicio ISR
                                        int AnioActual = DateTime.Now.Year;
                                        DateTime AnioInicio = new DateTime(DateTime.Now.Year, 1, 1);
                                        DateTime AnioFin = new DateTime(DateTime.Now.Year, 12, 31);
                                        DateTime AnioDC = new DateTime(DateTime.Now.Year, 6, 30);
                                        DateTime AnioDCA = AnioDC.AddYears(-1);
                                        int CantidadDiasPagarAnterior = 0;
                                        int MesesPagarAnterior = 0;
                                        int CantidadDiasPagarNuevo = 0;
                                        int MesesPagarNuevo = 0;
                                        decimal SueldoA = 0;
                                        decimal SueldoB = 0;
                                        decimal RentaNetaGravable = 0;
                                        decimal ExcesoDecimoTercer = 0;
                                        decimal ExcesoDecimoCuarto = 0;
                                        decimal SueldoBaseActual = 0;
                                        decimal SueldoNuevoA = 0;
                                        decimal SueldoAnteriorA = 0;
                                        decimal SueldoAnual = 0;
                                        //Fin ISR
                                        int horasTrabajadas = 0;
                                        decimal salarioHora = 0;
                                        decimal totalSalario = 0;
                                        string tipoPlanilla = string.Empty;
                                        decimal? porcentajeComision = 0;
                                        decimal? totalVentas = 0;
                                        decimal? totalComisiones = 0;
                                        int horasExtrasTrabajadas = 0;
                                        decimal? totalHorasExtras = 0;
                                        decimal? totalHorasPermiso = 0;
                                        decimal? totalBonificaciones = 0;
                                        decimal? totalVacaciones = 0;
                                        decimal? totalIngresosEmpleado = 0;
                                        decimal totalISR = 0;
                                        decimal? colaboradorDeducciones = 0;
                                        decimal totalAFP = 0;
                                        decimal? totalInstitucionesFinancieras = 0;
                                        decimal? totalOtrasDeducciones = 0;
                                        decimal? adelantosSueldo = 0;
                                        decimal? totalDeduccionesEmpleado = 0;
                                        decimal? netoAPagarColaborador = 0;
                                        //int VerificarHorasTrabajas = 0;
                                        oPlanillaEmpleado = new ReportePlanillaViewModel();
                                        //variables para insertar en los historiales de pago
                                        IEnumerable<object> listHistorialPago = null;
                                        string MensajeError = "";
                                        List<tbHistorialDeduccionPago> lisHistorialDeducciones = new List<tbHistorialDeduccionPago>();
                                        List<tbHistorialDeIngresosPago> lisHistorialIngresos = new List<tbHistorialDeIngresosPago>();


                                        #endregion

                                        #region Procesar ingresos

                                        //informacion del colaborador actual
                                        V_InformacionColaborador InformacionDelEmpleadoActual = db.V_InformacionColaborador.Where(x => x.emp_Id == empleadoActual.emp_Id).FirstOrDefault();

                                        //salario base del colaborador actual
                                        SalarioBase = Math.Round((Decimal)InformacionDelEmpleadoActual.SalarioBase, 2);

                                        //para el voucer
                                        ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                        {
                                            concepto = "Sueldo base",
                                            monto = SalarioBase
                                        });
                                        //Historial de ingresos
                                        lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                        {
                                            hip_UnidadesPagar = 1,
                                            hip_MedidaUnitaria = 1,
                                            hip_TotalPagar = SalarioBase,
                                            cin_IdIngreso = 7
                                        });

                                        //horas normales trabajadas
                                        horasTrabajadas = db.tbHistorialHorasTrabajadas
                                            .Where(x => x.emp_Id == empleadoActual.emp_Id && x.htra_Estado == true && x.tbTipoHoras.tiho_Recargo == 0)
                                            .Select(x => x.htra_CantidadHoras)
                                            .DefaultIfEmpty(0)
                                            .Sum();
                                        //para el voucer
                                        ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                        {
                                            concepto = "Horas trabajadas",
                                            monto = horasTrabajadas
                                        });

                                        //salario por hora
                                        salarioHora = Math.Round((Decimal)SalarioBase / 240, 2);

                                        //para el voucher
                                        ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                        {
                                            concepto = "Sueldo hora",
                                            monto = salarioHora
                                        });

                                        //total salario
                                        totalSalario = Math.Round((Decimal)salarioHora * horasTrabajadas, 2);
                                        //para el voucer
                                        ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                        {
                                            concepto = "Total sueldo",
                                            monto = totalSalario
                                        });
                                        //Historial de ingresos (horas normales trabajadas)
                                        lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                        {
                                            hip_UnidadesPagar = horasTrabajadas,
                                            hip_MedidaUnitaria = 1,
                                            hip_TotalPagar = totalSalario,
                                            cin_IdIngreso = 11
                                        });

                                        //horas con permiso justificado
                                        List<tbHistorialPermisos> horasConPermiso = db.tbHistorialPermisos
                                            .Where(x => x.emp_Id == empleadoActual.emp_Id && x.hper_Estado == true)
                                            .ToList();

                                        if (horasConPermiso.Count > 0)
                                        {
                                            int CantidadHorasPermisoActual = 0;
                                            //sumar todas las horas extras
                                            foreach (var iterHorasPermiso in horasConPermiso)
                                            {
                                                CantidadHorasPermisoActual = iterHorasPermiso.hper_Duracion;

                                                totalHorasPermiso += Math.Round(CantidadHorasPermisoActual * (((iterHorasPermiso.hper_PorcentajeIndemnizado * salarioHora) / 100)), 2);


                                                //para el voucher
                                                ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = $"{CantidadHorasPermisoActual} horas permiso indemnizado {iterHorasPermiso.hper_PorcentajeIndemnizado} %",
                                                    monto = Math.Round(CantidadHorasPermisoActual * (((iterHorasPermiso.hper_PorcentajeIndemnizado * salarioHora) / 100)), 2)
                                                });

                                                //Historial de ingresos (horas con permiso)
                                                lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                                {
                                                    hip_UnidadesPagar = CantidadHorasPermisoActual,
                                                    hip_MedidaUnitaria = 1,
                                                    hip_TotalPagar = Math.Round(CantidadHorasPermisoActual * (((iterHorasPermiso.hper_PorcentajeIndemnizado * salarioHora) / 100)), 2),
                                                    cin_IdIngreso = 12
                                                });
                                            }
                                        }

                                        //comisiones
                                        List<tbEmpleadoComisiones> oComisionesColaboradores = db.tbEmpleadoComisiones.Where(x => x.emp_Id == empleadoActual.emp_Id && x.cc_Activo == true && x.cc_Pagado == false).ToList();
                                        if (oComisionesColaboradores.Count > 0)
                                        {
                                            //sumar todas las comisiones
                                            foreach (var oComisionesColaboradoresIterador in oComisionesColaboradores)
                                            {
                                                porcentajeComision = (from tbEmpCom in db.tbEmpleadoComisiones
                                                                      where tbEmpCom.cc_Id == oComisionesColaboradoresIterador.cc_Id
                                                                      select tbEmpCom.cc_PorcentajeComision).FirstOrDefault();

                                                totalVentas = (from tbEmpCom in db.tbEmpleadoComisiones
                                                               where tbEmpCom.cc_Id == oComisionesColaboradoresIterador.cc_Id
                                                               select tbEmpCom.cc_TotalVenta).FirstOrDefault();

                                                totalComisiones += Math.Round((Decimal)(oComisionesColaboradoresIterador.cc_TotalVenta * oComisionesColaboradoresIterador.cc_PorcentajeComision) / 100, 2);

                                                //pasar el estado de las comisiones a pagadas
                                                oComisionesColaboradoresIterador.cc_Pagado = true;
                                                oComisionesColaboradoresIterador.cc_FechaPagado = DateTime.Now;
                                                db.Entry(oComisionesColaboradoresIterador).State = EntityState.Modified;

                                                //agregarlas al vocher
                                                ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = oComisionesColaboradoresIterador.tbCatalogoDeIngresos.cin_DescripcionIngreso,
                                                    monto = Math.Round((Decimal)(oComisionesColaboradoresIterador.cc_TotalVenta * oComisionesColaboradoresIterador.cc_PorcentajeComision) / 100, 2)
                                                });
                                                //Historial de ingresos (Comisiones)
                                                lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                                {
                                                    hip_UnidadesPagar = 1,
                                                    hip_MedidaUnitaria = 1,
                                                    hip_TotalPagar = Math.Round((Decimal)(oComisionesColaboradoresIterador.cc_TotalVenta * oComisionesColaboradoresIterador.cc_PorcentajeComision) / 100, 2),
                                                    cin_IdIngreso = 8
                                                });
                                            }
                                        }

                                        //horas extras
                                        horasExtrasTrabajadas = db.tbHistorialHorasTrabajadas
                                            .Where(x => x.emp_Id == empleadoActual.emp_Id && x.htra_Estado == true && x.tbTipoHoras.tiho_Recargo > 0)
                                            .Select(x => x.htra_CantidadHoras)
                                            .DefaultIfEmpty(0)
                                            .Sum();

                                        if (horasExtrasTrabajadas > 0)
                                        {
                                            //para el voucer
                                            ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                            {
                                                concepto = "Horas extras",
                                                monto = horasExtrasTrabajadas
                                            });
                                        }

                                        //total ingresos horas extras
                                        List<tbHistorialHorasTrabajadas> oHorasExtras = db.tbHistorialHorasTrabajadas
                                                                                        .Where(x => x.emp_Id == empleadoActual.emp_Id && x.htra_Estado == true && x.tbTipoHoras.tiho_Recargo > 0)
                                                                                        .ToList();
                                        if (oHorasExtras.Count > 0)
                                        {
                                            int CantidadHorasExtrasActual = 0;
                                            //sumar todas las horas extras
                                            foreach (var iterHorasExtras in oHorasExtras)
                                            {
                                                CantidadHorasExtrasActual = db.tbHistorialHorasTrabajadas
                                                .Where(x => x.emp_Id == empleadoActual.emp_Id && x.htra_Estado == true && x.htra_Id == iterHorasExtras.htra_Id)
                                                .Select(x => x.htra_CantidadHoras)
                                                .DefaultIfEmpty(0)
                                                .Sum();

                                                totalHorasExtras += Math.Round((Decimal)CantidadHorasExtrasActual * (salarioHora + ((iterHorasExtras.tbTipoHoras.tiho_Recargo * salarioHora) / 100)), 2);


                                                //para el voucher
                                                ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = $"{CantidadHorasExtrasActual} horas {iterHorasExtras.tbTipoHoras.tiho_Descripcion} al {iterHorasExtras.tbTipoHoras.tiho_Recargo} %",
                                                    monto = Math.Round((Decimal)CantidadHorasExtrasActual * (salarioHora + ((iterHorasExtras.tbTipoHoras.tiho_Recargo * salarioHora) / 100)), 2)
                                                });
                                                //Historial de ingresos (horas extras)
                                                lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                                {
                                                    hip_UnidadesPagar = 1,
                                                    hip_MedidaUnitaria = 1,
                                                    hip_TotalPagar = Math.Round((Decimal)CantidadHorasExtrasActual * (salarioHora + ((iterHorasExtras.tbTipoHoras.tiho_Recargo * salarioHora) / 100)), 2),
                                                    cin_IdIngreso = 3
                                                });
                                            }
                                            //para el voucher
                                            ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                            {
                                                concepto = "Total horas extras",
                                                monto = totalHorasExtras
                                            });
                                        }

                                        //bonos del colaborador
                                        List<tbEmpleadoBonos> oBonosColaboradores = db.tbEmpleadoBonos.Where(x => x.emp_Id == empleadoActual.emp_Id && x.cb_Activo == true && x.cb_Pagado == false).ToList();

                                        if (oBonosColaboradores.Count > 0)
                                        {
                                            //iterar los bonos
                                            foreach (var oBonosColaboradoresIterador in oBonosColaboradores)
                                            {
                                                totalBonificaciones += Math.Round((Decimal)oBonosColaboradoresIterador.cb_Monto, 2);
                                                //pasar el bono a pagado
                                                oBonosColaboradoresIterador.cb_Pagado = true;
                                                oBonosColaboradoresIterador.cb_FechaPagado = DateTime.Now;
                                                db.Entry(oBonosColaboradoresIterador).State = EntityState.Modified;

                                                //agregarlo al voucher
                                                ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = oBonosColaboradoresIterador.tbCatalogoDeIngresos.cin_DescripcionIngreso,
                                                    monto = Math.Round((Decimal)oBonosColaboradoresIterador.cb_Monto, 2)
                                                });
                                                //Historial de ingresos (bonos)
                                                lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                                {
                                                    hip_UnidadesPagar = 1,
                                                    hip_MedidaUnitaria = 1,
                                                    hip_TotalPagar = Math.Round((Decimal)oBonosColaboradoresIterador.cb_Monto, 2),
                                                    cin_IdIngreso = oBonosColaboradoresIterador.cin_IdIngreso
                                                });
                                            }
                                        }

                                        //vacaciones
                                        List<tbHistorialVacaciones> oVacacionesColaboradores = db.tbHistorialVacaciones.Where(x => x.emp_Id == empleadoActual.emp_Id && x.hvac_DiasPagados == false && x.hvac_Estado == true).ToList();
                                        if (oVacacionesColaboradores.Count > 0)
                                        {
                                            //sumar todas las comisiones
                                            foreach (var oVacacionesColaboradoresIterador in oVacacionesColaboradores)
                                            {
                                                int cantidadDias = 0;
                                                DateTime VacacionesfechaInicio;
                                                DateTime VacacionesfechaFin;

                                                VacacionesfechaInicio = (from tbEmpVac in db.tbHistorialVacaciones
                                                                         where tbEmpVac.hvac_Id == oVacacionesColaboradoresIterador.hvac_Id
                                                                         select tbEmpVac.hvac_FechaInicio).FirstOrDefault();

                                                VacacionesfechaFin = (from tbEmpVac in db.tbHistorialVacaciones
                                                                      where tbEmpVac.hvac_Id == oVacacionesColaboradoresIterador.hvac_Id
                                                                      select tbEmpVac.hvac_FechaFin).FirstOrDefault();

                                                TimeSpan restaFechas = VacacionesfechaFin - VacacionesfechaInicio;
                                                cantidadDias = restaFechas.Days;

                                                totalVacaciones += Math.Round((salarioHora * 8) * cantidadDias, 2);

                                                //cambiar el estado de las vacaciones a pagadas
                                                oVacacionesColaboradoresIterador.hvac_DiasPagados = true;
                                                db.Entry(oVacacionesColaboradoresIterador).State = EntityState.Modified;

                                                //agregarlas al vocher
                                                ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = $"{cantidadDias} dias de vacaciones",
                                                    monto = Math.Round((Decimal)(salarioHora * 8) * cantidadDias, 2)
                                                });
                                                //Historial de ingresos (vacaciones)
                                                lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                                {
                                                    hip_UnidadesPagar = cantidadDias,
                                                    hip_MedidaUnitaria = 1,
                                                    hip_TotalPagar = Math.Round((Decimal)(salarioHora * 8) * cantidadDias, 2),
                                                    cin_IdIngreso = 12
                                                });
                                            }
                                        }
                                        #region Septimo Dia

                                        ////horas normales trabajadas
                                        //VerificarHorasTrabajas = db.tbHistorialHorasTrabajadas
                                        //    .Where(x => x.emp_Id == empleadoActual.emp_Id && x.htra_Estado == true && x.tbTipoHoras.tiho_Recargo == 0)
                                        //    .Select(x => x.htra_CantidadHoras)
                                        //    .DefaultIfEmpty(0)
                                        //    .Sum();

                                        //////BUSCAR HORAS CON PERMISOS
                                        //int HorasPermiso = 0;
                                        //HorasPermiso = db.tbHistorialPermisos
                                        //    .Where(x => x.emp_Id == empleadoActual.emp_Id && x.hper_Estado == true)
                                        //    .Select(x => x.hper_Duracion)
                                        //    .DefaultIfEmpty(0)
                                        //    .Sum();

                                        //DateTime Inicio = fechaInicio;
                                        //DateTime Fin = fechaFin;
                                        //int Dia = 0;

                                        //for (int i = Inicio.Day; i <= Fin.Day; i++)
                                        //{
                                        //    tbHistorialHorasTrabajadas obj = db.tbHistorialHorasTrabajadas.Where(x => x.emp_Id == empleadoActual.emp_Id && x.htra_Fecha == Inicio.AddDays(Dia) && x.htra_Estado == true && x.tbTipoHoras.tiho_Recargo == 0).FirstOrDefault();
                                        //    if (obj.htra_CantidadHoras < 8)
                                        //    {

                                        //    }
                                        //    Dia++;
                                        //    if (Dia == 6) i++;
                                        //}---------

                                        //int cantidadSeptimoDias = oPlanilla.cpla_FrecuenciaEnDias / 7;
                                        //decimal totalHorasEmpleado = VerificarHorasTrabajas + HorasPermiso;
                                        //int contadorSeptimosDias = 0;

                                        //while (totalHorasEmpleado >= 48)
                                        //{
                                        //    if (totalHorasEmpleado - 48 >= 48)
                                        //    {
                                        //        totalHorasEmpleado = totalHorasEmpleado - 48;
                                        //        contadorSeptimosDias++;
                                        //    }
                                        //    else
                                        //        break;
                                        //    if (contadorSeptimosDias >= cantidadSeptimoDias) break;
                                        //}
                                        //totalSeptimoDia = Math.Round((decimal)salarioHora * (contadorSeptimosDias * 8), 2);

                                        //if (totalSeptimoDia > 0)
                                        //{
                                        //    //agregarlas al vocher
                                        //    ListaIngresosVoucher.Add(new IngresosDeduccionesVoucher
                                        //    {
                                        //        concepto = $"{contadorSeptimosDias}x Séptimo día",
                                        //        monto = totalSeptimoDia
                                        //    });
                                        //    //Historial de ingresos (septimos dias)
                                        //    lisHistorialIngresos.Add(new tbHistorialDeIngresosPago
                                        //    {
                                        //        hip_UnidadesPagar = contadorSeptimosDias,
                                        //        hip_MedidaUnitaria = 1,
                                        //        hip_TotalPagar = Math.Round((decimal)totalSeptimoDia, 2),
                                        //        cin_IdIngreso = 1
                                        //    });
                                        //}

                                        #endregion

                                        //total ingresos
                                        totalIngresosEmpleado = totalSalario + totalComisiones + totalHorasExtras + totalBonificaciones + totalVacaciones + totalHorasPermiso;

                                        #endregion

                                        #region Procesar deducciones

                                        #region Primeras Deducciones
                                        //deducciones de la planilla
                                        foreach (var iterDeducciones in oDeducciones)
                                        {
                                            decimal? porcentajeColaborador = iterDeducciones.cde_PorcentajeColaborador;
                                            decimal? porcentajeEmpresa = iterDeducciones.cde_PorcentajeEmpresa;

                                            //verificar techos deducciones
                                            List<tbTechosDeducciones> oTechosDeducciones = db.tbTechosDeducciones.Where(x => x.cde_IdDeducciones == iterDeducciones.cde_IdDeducciones && x.tddu_Activo == true).OrderBy(x => x.tddu_Techo).ToList();
                                            if (oTechosDeducciones.Count() > 0)
                                            {
                                                foreach (var techosDeduccionesIter in oTechosDeducciones)
                                                {
                                                    if (SalarioBase > techosDeduccionesIter.tddu_Techo)
                                                    {
                                                        porcentajeColaborador = techosDeduccionesIter.tddu_PorcentajeColaboradores;
                                                        porcentajeEmpresa = techosDeduccionesIter.tddu_PorcentajeEmpresa;
                                                    }
                                                }
                                            }
                                            //sumar las deducciones
                                            colaboradorDeducciones += Math.Round((decimal)(SalarioBase * porcentajeColaborador) / 100, 2);
                                            //Voucher
                                            ListaDeduccionesVoucher.Add(new IngresosDeduccionesVoucher
                                            {
                                                concepto = iterDeducciones.cde_DescripcionDeduccion,
                                                monto = Math.Round((decimal)(SalarioBase * porcentajeColaborador) / 100, 2)
                                            });

                                            //Historial de deducciones
                                            lisHistorialDeducciones.Add(new tbHistorialDeduccionPago
                                            {
                                                cde_IdDeducciones = iterDeducciones.cde_IdDeducciones,
                                                hidp_Total = Math.Round((decimal)(SalarioBase * porcentajeColaborador) / 100, 2)
                                            });
                                        }

                                        //instituciones financieras
                                        List<tbDeduccionInstitucionFinanciera> oDeduInstiFinancieras = db.tbDeduccionInstitucionFinanciera.Where(x => x.emp_Id == empleadoActual.emp_Id && x.deif_Activo == true && x.deif_Pagado == false).ToList();

                                        if (oDeduInstiFinancieras.Count > 0)
                                        {
                                            //sumarlas todas
                                            foreach (var oDeduInstiFinancierasIterador in oDeduInstiFinancieras)
                                            {
                                                totalInstitucionesFinancieras += Math.Round((decimal)oDeduInstiFinancierasIterador.deif_Monto, 2);
                                                //pasar el estado de la deduccion a pagada
                                                oDeduInstiFinancierasIterador.deif_Pagado = true;
                                                db.Entry(oDeduInstiFinancierasIterador).State = EntityState.Modified;

                                                ListaDeduccionesVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = $"{oDeduInstiFinancierasIterador.tbInstitucionesFinancieras.insf_DescInstitucionFinanc} {oDeduInstiFinancierasIterador.deif_Comentarios}",
                                                    monto = Math.Round((decimal)oDeduInstiFinancierasIterador.deif_Monto, 2)
                                                });
                                                //Historial de deducciones
                                                lisHistorialDeducciones.Add(new tbHistorialDeduccionPago
                                                {
                                                    cde_IdDeducciones = oDeduInstiFinancierasIterador.cde_IdDeducciones,
                                                    hidp_Total = Math.Round((decimal)oDeduInstiFinancierasIterador.deif_Monto, 2)
                                                });
                                            }
                                        }
                                        //afp
                                        List<tbDeduccionAFP> oDeduccionAfp = db.tbDeduccionAFP.Where(af => af.emp_Id == empleadoActual.emp_Id && af.dafp_Pagado == false && af.dafp_Activo == true).ToList();

                                        if (oDeduccionAfp.Count > 0)
                                        {
                                            //sumarlas todas
                                            foreach (var oDeduccionAfpIter in oDeduccionAfp)
                                            {
                                                totalAFP += Math.Round((decimal)oDeduccionAfpIter.dafp_AporteLps, 2);
                                                //pasar el estado del aporte a pagado
                                                oDeduccionAfpIter.dafp_Pagado = true;
                                                db.Entry(oDeduccionAfpIter).State = EntityState.Modified;

                                                ListaDeduccionesVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = oDeduccionAfpIter.tbAFP.afp_Descripcion,
                                                    monto = Math.Round(oDeduccionAfpIter.dafp_AporteLps, 2)
                                                });
                                            }
                                        }

                                        //deducciones extras
                                        List<tbDeduccionesExtraordinarias> oDeduccionesExtrasColaborador = db.tbDeduccionesExtraordinarias.Where(DEX => DEX.tbEquipoEmpleados.emp_Id == empleadoActual.emp_Id && DEX.dex_MontoRestante > 0 && DEX.dex_Activo == true).ToList();

                                        if (oDeduccionesExtrasColaborador.Count > 0)
                                        {
                                            //sumarlas todas
                                            foreach (var oDeduccionesExtrasColaboradorIterador in oDeduccionesExtrasColaborador)
                                            {
                                                totalOtrasDeducciones += oDeduccionesExtrasColaboradorIterador.dex_MontoRestante <= oDeduccionesExtrasColaboradorIterador.dex_Cuota ? oDeduccionesExtrasColaboradorIterador.dex_MontoRestante : oDeduccionesExtrasColaboradorIterador.dex_Cuota;
                                                //restar la cuota al monto restante
                                                oDeduccionesExtrasColaboradorIterador.dex_MontoRestante = oDeduccionesExtrasColaboradorIterador.dex_MontoRestante <= oDeduccionesExtrasColaboradorIterador.dex_Cuota ? oDeduccionesExtrasColaboradorIterador.dex_MontoRestante - oDeduccionesExtrasColaboradorIterador.dex_MontoRestante : oDeduccionesExtrasColaboradorIterador.dex_MontoRestante - oDeduccionesExtrasColaboradorIterador.dex_Cuota;
                                                db.Entry(oDeduccionesExtrasColaboradorIterador).State = EntityState.Modified;

                                                ListaDeduccionesVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = oDeduccionesExtrasColaboradorIterador.dex_ObservacionesComentarios,
                                                    monto = Math.Round((decimal)oDeduccionesExtrasColaboradorIterador.dex_Cuota, 2)
                                                });

                                                //Historial de deducciones
                                                lisHistorialDeducciones.Add(new tbHistorialDeduccionPago
                                                {
                                                    cde_IdDeducciones = oDeduccionesExtrasColaboradorIterador.cde_IdDeducciones,
                                                    hidp_Total = Math.Round((decimal)oDeduccionesExtrasColaboradorIterador.dex_Cuota, 2)
                                                });
                                            }
                                        }

                                        //adelantos de sueldo
                                        List<tbAdelantoSueldo> oAdelantosSueldo = db.tbAdelantoSueldo.Where(x => x.emp_Id == empleadoActual.emp_Id && x.adsu_Activo == true && x.adsu_Deducido == false).ToList();

                                        if (oAdelantosSueldo.Count > 0)
                                        {
                                            //sumarlas todas
                                            foreach (var oAdelantosSueldoIterador in oAdelantosSueldo)
                                            {
                                                adelantosSueldo += Math.Round((decimal)oAdelantosSueldoIterador.adsu_Monto, 2);
                                                //pasar el estado del adelanto a deducido
                                                oAdelantosSueldoIterador.adsu_Deducido = true;
                                                db.Entry(oAdelantosSueldoIterador).State = EntityState.Modified;

                                                ListaDeduccionesVoucher.Add(new IngresosDeduccionesVoucher
                                                {
                                                    concepto = $"Adelanto sueldo ({oAdelantosSueldoIterador.adsu_RazonAdelanto})",
                                                    monto = Math.Round((decimal)oAdelantosSueldoIterador.adsu_Monto, 2)
                                                });
                                                //Historial de deducciones
                                                lisHistorialDeducciones.Add(new tbHistorialDeduccionPago
                                                {
                                                    cde_IdDeducciones = 9,
                                                    hidp_Total = Math.Round((decimal)oAdelantosSueldoIterador.adsu_Monto, 2)
                                                });
                                            }
                                        }
                                        #endregion

                                        //ISR


                                        //Hacer la proyeccion del sueldo
                                        //for (int i = 1; i <= anioActualEnero.Month; i++)
                                        //{

                                        //}




                                        #region Codigo anterior
                                        //Saber si al empleado se le ha cambiado el sueldo en el año
                                        //if (sueldoFechaCrea.idSueldoAnterior != null && sueldoFechaCrea.fechaModifica != null)
                                        //{
                                        //    var sueldoAnteriorFechaCrea = db.tbSueldos.Where(x => x.sue_Id == sueldoFechaCrea.idSueldoAnterior).Select(x => new { sueldo = x.sue_Cantidad, fechaCrea = (DateTime)x.sue_FechaCrea }).FirstOrDefault();

                                        //    sueldoAnteriorFechaCambio = sueldoAnteriorFechaCrea.fechaCrea;

                                        //    //Si es actual el cambio de sueldo
                                        //    if (anioActual > sueldoAnteriorFechaCambio)
                                        //    {
                                        //        sueldoAnterior = sueldoAnteriorFechaCrea.sueldo;
                                        //        haCambiadoSueldo = true;
                                        //    }
                                        //}

                                        //decimal salarioAntesDeCambioSueldo = 0;
                                        //decimal salarioDespuesDeCambioSueldo = 0;
                                        //decimal totalSueldoAnualConCambioSueldo = 0;
                                        //if (haCambiadoSueldo)
                                        //{

                                        //    //Calcular el salario anual antes del cambio de sueldo
                                        //    for (int i = 1; i <= sueldoAnteriorFechaCambio.Month; i++)
                                        //    {
                                        //        salarioAntesDeCambioSueldo += sueldoActual;
                                        //    }

                                        //    //Calcular el salario anual despues de que se le cambio el sueldo
                                        //    for(int i = sueldoAnteriorFechaCambio.Month; i <= 12; i++)
                                        //    {
                                        //        salarioDespuesDeCambioSueldo += sueldoAnterior;
                                        //    }

                                        //    totalSueldoAnualConCambioSueldo = salarioAntesDeCambioSueldo + salarioDespuesDeCambioSueldo;
                                        //}
                                        //else
                                        //{
                                        //    //sueldo*12
                                        //}
                                        #endregion

                                        //Salario minimo de 10 meses

                                        //Exceso de 14 avo

                                        //Exceso de 13 avo

                                        //Exceso de vacaciones

                                        //Traemos la Fecha de Ingreso del Empleado para saber cuantos meses lleva laborando
                                        //var FechaIngresoEmp = oEmpleado.emp_Fechaingreso;

                                        ////Se calculan los meses laborados del Empleado
                                        //var Anios = AnioFin.Year - FechaIngresoEmp.Year;
                                        //var Meses = Math.Round((Decimal)Anios / 12, 0);

                                        ////Verificar si tiene un Sueldo Anterior al Actual
                                        //if (tablaSueldos.sue_SueldoAnterior != null)
                                        //{
                                        //    if (tablaSueldos.sue_FechaModifica != null)
                                        //    {
                                        //        var SueldoAnterior = db.tbSueldos.Where(x => x.sue_Id == tablaSueldos.sue_SueldoAnterior)
                                        //            .Select(x => new { CantidadAnterior = x.sue_Cantidad, FechaCrea = x.sue_FechaCrea }).FirstOrDefault();

                                        //        var SueldoNuevo = db.tbSueldos.Where(x => x.sue_Id == tablaSueldos.sue_Cantidad)
                                        //            .Select(x => new { Cantidad = x.sue_Cantidad, FechaModifica = x.sue_FechaModifica }).FirstOrDefault();

                                        //        if (SueldoNuevo.FechaCrea >= AnioInicio)
                                        //        {
                                        //            //Calcular los otros 9 meses mas en base al sueldo nuevo

                                        //            //Sacar la cantidad de meses a pagar desde que se le cambio el sueldo
                                        //            CantidadDiasPagarNuevo = ((TimeSpan)(tablaSueldos.sue_FechaModifica - AnioInicio)).Days;

                                        //            while (CantidadDiasPagarNuevo >= 30)
                                        //            {
                                        //                CantidadDiasPagarNuevo -= 30;
                                        //                MesesPagar += 1;
                                        //            }

                                        //            SueldoNuevoA = SueldoAnterior.CantidadAnterior * MesesPagar;
                                        //        }

                                        //        if (SueldoNuevo.FechaModifica >= AnioInicio)
                                        //        {
                                        //            CantidadDiasPagar
                                        //                if (Meses > 12)
                                        //            {
                                        //                //Será sueldo por el Año Inicio menos el Año Fin Actuales
                                        //                SueldoAnteriorA = (Decimal)Ca * (Convert.ToInt32(AnioFin - AnioInicio));
                                        //            }
                                        //            else
                                        //            {
                                        //                //Sino sueldo por los meses laborados
                                        //                SueldoAnteriorA = tablaSueldos.sue_Cantidad * Meses;
                                        //            }
                                        //        }
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    //En caso de que no sea del año presente y exceda los 12 meses del año
                                        //    if (Meses > 12)
                                        //    {
                                        //        //Será sueldo por el Año Inicio menos el Año Fin Actuales
                                        //        SueldoAnual = tablaSueldos.sue_Cantidad * (Convert.ToInt32(AnioFin - AnioInicio));
                                        //    }
                                        //    else
                                        //    {
                                        //        //Sino sueldo por los meses laborados
                                        //        SueldoAnual = tablaSueldos.sue_Cantidad * Meses;
                                        //    }
                                        //    //Y al final tenemos el Sueldo Anual del Empleado
                                        //}


                                        //-----------------------------------------------------------------------------------------------------------------------------


                                        //-----------------------------------------------------------------------------------------------------------------------------
                                        //Exceso Décimo Tercer Mes
                                        List<V_DecimoTercerMes_Pagados> DecimoTercer = db.V_DecimoTercerMes_Pagados.Where(x => x.emp_Id == empleadoActual.emp_Id).ToList();
                                        List<tbSueldos> Sueldos = db.tbSueldos.Where(x => x.emp_Id == empleadoActual.emp_Id).ToList();
                                        List<tbEmpleados> tablaEmpleado = db.tbEmpleados.Where(x => x.emp_Id == empleadoActual.emp_Id).ToList();
                                        foreach (var oDecimo in DecimoTercer)
                                        {
                                            if (AnioActual == Convert.ToInt32(oDecimo.dtm_FechaPago.Year))
                                            {
                                                foreach (var oSueldo in Sueldos)
                                                {
                                                    foreach (var oEmpleado in tablaEmpleado)
                                                    {
                                                        //--Es posible no ser necesario repetir lo mismo...

                                                        //Traemos la Fecha de Ingreso del Empleado para saber cuantos meses lleva laborando
                                                        var FechaIngresoEmp = oEmpleado.emp_Fechaingreso;

                                                        //Se calculan los meses laborados del Empleado
                                                        var Meses = (Convert.ToInt32(AnioFin - FechaIngresoEmp));

                                                        //En caso de que no sea del año presente y exceda los 12 meses del año
                                                        if (Meses > 12)
                                                        {
                                                            //Será sueldo por el Año Inicio menos el Año Fin Actuales
                                                            SueldoA = oSueldo.sue_Cantidad * (Convert.ToInt32(AnioFin - AnioInicio));
                                                        }
                                                        else
                                                        {
                                                            //Sino sueldo por los meses laborados
                                                            SueldoA = oSueldo.sue_Cantidad * Meses;
                                                        }

                                                        //Comparamos el Sueldo de Meses Trabajados con el Décimo Tercer del Empleado
                                                        if (oDecimo.dtm_Monto > SueldoA)
                                                        {
                                                            //Si es mayor el Décimo Tercer se le resta el Sueldo de Meses Trabajados
                                                            ExcesoDecimoTercer = Convert.ToDecimal(oDecimo.dtm_Monto) - SueldoA;
                                                        }
                                                        else
                                                        {
                                                            //Sino simplemente será igual a Cero
                                                            ExcesoDecimoTercer = 0;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        //-----------------------------------------------------------------------------------------------------------------------------


                                        //-----------------------------------------------------------------------------------------------------------------------------
                                        //Exceso Décimo Cuarto Mes
                                        List<V_DecimoCuartoMes_Pagados> DecimoCuarto = db.V_DecimoCuartoMes_Pagados.Where(x => x.emp_Id == empleadoActual.emp_Id).ToList();
                                        List<tbSueldos> SueldoCan = db.tbSueldos.OrderByDescending(x => x.sue_Cantidad).ToList();
                                        List<tbEmpleados> Empleado = db.tbEmpleados.OrderByDescending(x => x.emp_Fechaingreso).ToList();
                                        foreach (var oDecimos in DecimoCuarto)
                                        {
                                            if (AnioActual == Convert.ToInt32(oDecimos.dcm_FechaPago.Year))
                                            {
                                                foreach (var oSueldos in SueldoCan)
                                                {
                                                    foreach (var oEmpleado in Empleado)
                                                    {
                                                        //Traemos la Fecha de Ingreso del Empleado para saber cuantos meses lleva laborando
                                                        var FechaIngresoEmp = oEmpleado.emp_Fechaingreso;

                                                        //Se calculan los meses laborados del Empleado
                                                        var Meses = (Convert.ToInt32(AnioFin - FechaIngresoEmp));

                                                        //En caso de que no sea del año presente y exceda los 12 meses del año
                                                        if (Meses > 12)
                                                        {
                                                            //Será sueldo por el Año Actual (30 de Junio), menos el Año Anterior a esa fecha
                                                            SueldoB = oSueldos.sue_Cantidad * (Convert.ToInt32(AnioDC - AnioDCA));
                                                        }
                                                        else
                                                        {
                                                            //Sino sueldo por los meses laborados
                                                            SueldoB = oSueldos.sue_Cantidad * Meses;
                                                        }

                                                        //Comparamos el Sueldo de Meses Trabajados con el Décimo Cuarto del Empleado
                                                        if (oDecimos.dcm_Monto > SueldoB)
                                                        {
                                                            //Si es mayor el Décimo Cuarto se le resta el Sueldo de Meses Trabajados
                                                            ExcesoDecimoCuarto = Convert.ToDecimal(oDecimos.dcm_Monto) - SueldoB;
                                                        }
                                                        else
                                                        {
                                                            //Sino simplemente será igual a Cero
                                                            ExcesoDecimoCuarto = 0;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        //-----------------------------------------------------------------------------------------------------------------------------


                                        //-----------------------------------------------------------------------------------------------------------------------------
                                        //Exceso Vacaciones



                                        //-----------------------------------------------------------------------------------------------------------------------------
                                        //Total Ingresos Gravables


                                        //Total Deducciones


                                        //Renta Neta Gravable


                                        //Cálculo con la Tabla Progresiva
                                        tbISR oRangoInicial = db.tbISR.Take(1).OrderByDescending(x => x.isr_RangoInicial).FirstOrDefault();
                                        tbISR oRangoFinal = db.tbISR.Take(1).OrderByDescending(x => x.isr_RangoFinal).FirstOrDefault();
                                        List<tbISR> tablaProgresiva = db.tbISR.OrderByDescending(x => x.isr_RangoInicial).ToList();

                                        foreach (var objTablaProgresiva in tablaProgresiva)
                                        {
                                            if (RentaNetaGravable > objTablaProgresiva.isr_RangoInicial)
                                            {



                                            }
                                        }




                                        //totales
                                        totalDeduccionesEmpleado = Math.Round((decimal)totalOtrasDeducciones, 2) + totalInstitucionesFinancieras + colaboradorDeducciones + totalAFP;
                                        netoAPagarColaborador = totalIngresosEmpleado - totalDeduccionesEmpleado;

                                        #endregion

                                        #region Calcular ISR

                                        const double SALARIO_MINIMO = 9433.24;

                                        var tablaEmp = db.tbSueldos.Where(x => x.emp_Id == empleadoActual.emp_Id).OrderBy(x => x.sue_FechaCrea);

                                        //Sueldo del Colaborador Actual
                                        var sueldoFechaCrea = tablaEmp.Select(x => new { sueldo = x.sue_Cantidad, fechaCrea = (DateTime)x.sue_FechaCrea, idSueldoAnterior = x.sue_SueldoAnterior, fechaModifica = x.sue_FechaModifica }).FirstOrDefault();
                                        decimal sueldoActual = sueldoFechaCrea.sueldo;
                                        //Sueldo redondeado del Colaborador
                                        SueldoBaseActual = Math.Round(sueldoActual, 2);
                                        DateTime anioActualEnero = new DateTime(DateTime.Now.Year, 1, 1);
                                        bool entroEsteAnio = false;
                                        decimal SueldoAnualISR = 0;

                                        //Si entro en cualquier fecha de este año hacer proyeccion de los meses que no se le pago

                                        var datosIngresoEmpleado = db.tbHistorialDePago.Where(x => x.emp_Id == empleadoActual.emp_Id && x.hipa_Anio == anioActualEnero.Year).Select(x => new { sueldoNeto = x.hipa_SueldoNeto, mes = x.hipa_Mes }).ToList();

                                        //Obtener los pagos mensuales totales
                                        var mesesPago = (db.tbHistorialDePago
                                            .Where(x => x.emp_Id == empleadoActual.emp_Id && x.hipa_Anio == anioActualEnero.Year)
                                            .OrderBy(x => x.hipa_Mes)
                                            .GroupBy(x => x.hipa_Mes).Select(x => x.Sum(y => (Decimal)y.hipa_SueldoNeto))).ToList();


                                        DateTime fechaIngresoEmpleado = db.tbEmpleados.Where(x => x.emp_Id == empleadoActual.emp_Id).Select(x => x.emp_Fechaingreso).FirstOrDefault();
                                        bool esMensual = false;

                                        TimeSpan diferencia = anioActualEnero - fechaIngresoEmpleado;

                                        if(TimeSpan.Zero > diferencia)
                                            entroEsteAnio = true;

                                        //Saber que mes entro
                                        int mes = fechaIngresoEmpleado.Month;
                                        decimal SalarioPromedioAnualPagadoAlAnio = 0;
                                        decimal salarioPromedioAnualPagadoAlMes = 0;
                                        decimal totalSalarioAnual = SalarioPromedioAnualISR(netoAPagarColaborador,
                                            ref SueldoAnualISR,
                                            mesesPago,
                                            esMensual,
                                            ref SalarioPromedioAnualPagadoAlAnio,
                                            ref salarioPromedioAnualPagadoAlMes);

                                        #endregion

                                        #region crear registro de la planilla del colaborador para el reporte

                                        oPlanillaEmpleado.CodColaborador = InformacionDelEmpleadoActual.emp_Id.ToString();
                                        oPlanillaEmpleado.NombresColaborador = $"{empleadoActual.tbPersonas.per_Nombres} {empleadoActual.tbPersonas.per_Apellidos}";
                                        oPlanillaEmpleado.SalarioBase = SalarioBase;
                                        oPlanillaEmpleado.horasTrabajadas = horasTrabajadas;
                                        oPlanillaEmpleado.SalarioHora = salarioHora;
                                        oPlanillaEmpleado.totalSalario = totalSalario;
                                        oPlanillaEmpleado.tipoPlanilla = empleadoActual.tbCatalogoDePlanillas.cpla_DescripcionPlanilla;
                                        oPlanillaEmpleado.procentajeComision = porcentajeComision;
                                        oPlanillaEmpleado.totalVentas = totalVentas;
                                        oPlanillaEmpleado.totalComisiones = totalComisiones;
                                        oPlanillaEmpleado.horasExtras = horasExtrasTrabajadas;
                                        oPlanillaEmpleado.totalHorasPermiso = totalHorasPermiso;
                                        oPlanillaEmpleado.TotalIngresosHorasExtras = totalHorasExtras;
                                        oPlanillaEmpleado.totalBonificaciones = totalBonificaciones;
                                        oPlanillaEmpleado.totalVacaciones = totalVacaciones;
                                        oPlanillaEmpleado.totalIngresos = Math.Round((decimal)totalIngresosEmpleado, 2);
                                        oPlanillaEmpleado.totalISR = 0;
                                        oPlanillaEmpleado.totalDeduccionesColaborador = colaboradorDeducciones;
                                        oPlanillaEmpleado.totalAFP = totalAFP;
                                        oPlanillaEmpleado.totalInstitucionesFinancieras = totalInstitucionesFinancieras;
                                        oPlanillaEmpleado.otrasDeducciones = Math.Round((decimal)totalOtrasDeducciones, 2);
                                        oPlanillaEmpleado.adelantosSueldo = Math.Round((decimal)adelantosSueldo, 2);
                                        oPlanillaEmpleado.totalDeducciones = Math.Round((decimal)totalDeduccionesEmpleado, 2);
                                        oPlanillaEmpleado.totalAPagar = Math.Round((decimal)netoAPagarColaborador, 2);
                                        reporte.Add(oPlanillaEmpleado);
                                        oPlanillaEmpleado = null;
                                        #endregion

                                        #region agregar registro al excel                                        

                                        //agregar registroo a la hoja de excel
                                        dt.Rows.Add(empleadoActual.tbPersonas.per_Nombres,
                                                    empleadoActual.tbPersonas.per_Apellidos,
                                                    SalarioBase,
                                                    totalBonificaciones,
                                                    totalComisiones,
                                                    totalOtrasDeducciones,
                                                    totalInstitucionesFinancieras,
                                                    0,
                                                    0,
                                                    0,
                                                    0,
                                                    netoAPagarColaborador);
                                        #endregion

                                        #region Enviar comprobante de pago por email
                                        if (enviarEmail != null && enviarEmail == true)
                                        {
                                            oComprobantePagoModel.EmailAsunto = "Comprobante de pago";
                                            oComprobantePagoModel.NombreColaborador = empleadoActual.tbPersonas.per_Nombres + " " + empleadoActual.tbPersonas.per_Apellidos;
                                            oComprobantePagoModel.idColaborador = empleadoActual.emp_Id;
                                            oComprobantePagoModel.EmailDestino = empleadoActual.tbPersonas.per_CorreoElectronico;
                                            oComprobantePagoModel.PeriodoPago = $"{fechaInicio.ToString("dd/MM/yyyy")}- {fechaFin.ToString("dd/MM/yyyy")}";
                                            oComprobantePagoModel.Ingresos = ListaIngresosVoucher;
                                            oComprobantePagoModel.Deducciones = ListaDeduccionesVoucher;
                                            oComprobantePagoModel.totalIngresos = totalIngresosEmpleado;
                                            oComprobantePagoModel.totalDeducciones = totalDeduccionesEmpleado;
                                            oComprobantePagoModel.NetoPagar = netoAPagarColaborador;

                                            //Enviar comprobante de pago
                                            try
                                            {
                                                if (!utilities.SendEmail(oComprobantePagoModel))
                                                    errores++;
                                                else
                                                {
                                                    ListaDeduccionesVoucher = new List<IngresosDeduccionesVoucher>();
                                                    ListaIngresosVoucher = new List<IngresosDeduccionesVoucher>();
                                                }

                                            }
                                            catch (Exception ex)
                                            {
                                                errores++;
                                            }
                                        }
                                        #endregion

                                        #region guardar en el historial de pago                                     

                                        tbHistorialDePago oHistorialPagoEncabezado = new tbHistorialDePago();
                                        oHistorialPagoEncabezado.hipa_IdHistorialDePago = db.tbHistorialDePago.Max(x => x.hipa_IdHistorialDePago) + contador;
                                        oHistorialPagoEncabezado.emp_Id = empleadoActual.emp_Id;
                                        oHistorialPagoEncabezado.hipa_SueldoNeto = Math.Round((decimal)netoAPagarColaborador, 2);
                                        oHistorialPagoEncabezado.hipa_FechaInicio = fechaInicio;
                                        oHistorialPagoEncabezado.hipa_FechaFin = fechaFin;
                                        oHistorialPagoEncabezado.hipa_FechaPago = DateTime.Now;
                                        oHistorialPagoEncabezado.hipa_Anio = DateTime.Now.Year;
                                        oHistorialPagoEncabezado.hipa_Mes = DateTime.Now.Month;
                                        oHistorialPagoEncabezado.peri_IdPeriodo = 1;
                                        oHistorialPagoEncabezado.hipa_UsuarioCrea = 1;
                                        oHistorialPagoEncabezado.hipa_FechaCrea = DateTime.Now;
                                        oHistorialPagoEncabezado.hipa_TotalISR = totalISR;
                                        oHistorialPagoEncabezado.hipa_ISRPendiente = true;
                                        oHistorialPagoEncabezado.hipa_AFP = totalAFP;

                                        //db.tbHistorialDePago.Add(oHistorialPagoEncabezado);
                                        //Ejecutar el procedimiento almacenado
                                        listHistorialPago = db.UDP_Plani_tbHistorialDePago_Insert(oHistorialPagoEncabezado.emp_Id,
                                                                                                oHistorialPagoEncabezado.hipa_SueldoNeto,
                                                                                                oHistorialPagoEncabezado.hipa_FechaInicio,
                                                                                                oHistorialPagoEncabezado.hipa_FechaFin,
                                                                                                oHistorialPagoEncabezado.hipa_FechaPago,
                                                                                                oHistorialPagoEncabezado.hipa_Anio,
                                                                                                oHistorialPagoEncabezado.hipa_Mes,
                                                                                                oHistorialPagoEncabezado.peri_IdPeriodo,
                                                                                                oHistorialPagoEncabezado.hipa_UsuarioCrea,
                                                                                                oHistorialPagoEncabezado.hipa_FechaCrea,
                                                                                                oHistorialPagoEncabezado.hipa_TotalISR,
                                                                                                oHistorialPagoEncabezado.hipa_ISRPendiente,
                                                                                                oHistorialPagoEncabezado.hipa_AFP);

                                        //RECORRER EL TIPO COMPLEJO DEL PROCEDIMIENTO ALMACENADO PARA EVALUAR EL RESULTADO DEL SP
                                        foreach (UDP_Plani_tbHistorialDePago_Insert_Result Resultado in listHistorialPago)
                                            MensajeError = Resultado.MensajeError;


                                        if (MensajeError.StartsWith("-1"))
                                        {
                                            //EN CASO DE OCURRIR UN ERROR, IGUALAMOS LA VARIABLE "RESPONSE" A ERROR PARA VALIDARLO EN EL CLIENTE
                                            dt.Rows.Add(empleadoActual.tbPersonas.per_Nombres + ' ' + empleadoActual.tbPersonas.per_Apellidos,
                                                    "Ocurrió un error al generar la planilla de este empleado.");
                                            errores++;
                                        }
                                        //si el encabezado del historial de pago se registró correctamente, guardar los detalles
                                        else
                                        {
                                            //guardar en el detalle de deducciones del historial de pago
                                            foreach (var hisorialDeduccioneIterado in lisHistorialDeducciones)
                                            {
                                                int idDetalle = db.tbHistorialDeduccionPago.DefaultIfEmpty().Max(x => x.hidp_IdHistorialdeDeduPago);
                                                hisorialDeduccioneIterado.hidp_IdHistorialdeDeduPago = idDetalle != null ? idDetalle + idDetalleDeduccionHisotorialesContador : 1;
                                                hisorialDeduccioneIterado.hipa_IdHistorialDePago = int.Parse(MensajeError);
                                                hisorialDeduccioneIterado.hidp_UsuarioCrea = 1;
                                                hisorialDeduccioneIterado.hidp_FechaCrea = DateTime.Now;
                                                db.tbHistorialDeduccionPago.Add(hisorialDeduccioneIterado);
                                                idDetalleDeduccionHisotorialesContador++;

                                            }
                                            //guardar en el detalle de ingresos del historial de pago
                                            foreach (var hisorialIngresosIterado in lisHistorialIngresos)
                                            {
                                                int idDetalle = db.tbHistorialDeIngresosPago.DefaultIfEmpty().Max(x => x.hip_IdHistorialDeIngresosPago);
                                                hisorialIngresosIterado.hip_IdHistorialDeIngresosPago = idDetalle != null ? idDetalle + idDetalleIngresoHisotorialesContador : 1;
                                                hisorialIngresosIterado.hipa_IdHistorialDePago = int.Parse(MensajeError);
                                                hisorialIngresosIterado.hip_FechaInicio = fechaInicio;
                                                hisorialIngresosIterado.hip_FechaFinal = fechaFin;
                                                hisorialIngresosIterado.hip_UsuarioCrea = 1;
                                                hisorialIngresosIterado.hip_FechaCrea = DateTime.Now;
                                                db.tbHistorialDeIngresosPago.Add(hisorialIngresosIterado);
                                                idDetalleIngresoHisotorialesContador++;
                                            }
                                        }


                                        contador++;
                                        #endregion

                                        //guardar cambios en la bbdd
                                        db.SaveChanges();
                                        dbContextTransaccion.Commit();
                                    }
                                    //catch por si hubo un error al generar la planilla de un empleado
                                    catch (Exception ex)
                                    {
                                        // SI ALGO FALLA, HACER UN ROLLBACK
                                        dbContextTransaccion.Rollback();
                                        // mensaje del error en el registro del colaborador
                                        dt.Rows.Add(empleadoActual.tbPersonas.per_Nombres + ' ' + empleadoActual.tbPersonas.per_Apellidos,
                                                    "Ocurrió un error al generar la planilla de este empleado.");
                                        errores++;

                                    }
                                }
                            }

                        }
                        //catch por si se produjo un error al procesar una sola planilla
                        catch (Exception ex)
                        {

                            errores++;
                            dt.Rows.Add($"Ocurrió un error al generar la planilla {nombrePlanilla}.");
                        }
                    }

                }

                //enviar resultado al cliente
                response.Response = $"El proceso de generación de planilla se realizó, con {errores} errores";
                response.Encabezado = "Exito";
                response.Tipo = "success";
                //guardar archivo excel
                try
                {
                    oSLDocument.ImportDataTable(1, 1, dt, true);
                    oSLDocument.SaveAs(direccion);
                }
                catch (Exception ex)
                {
                    response.Response = "Planilla generada, error al crear documento excel.";
                    response.Encabezado = "Advertencia";
                    response.Tipo = "warning";
                }

            }
            // catch por si se produjo un error fatal en el proceso generar planilla
            catch (Exception ex)
            {
                response.Response = "El proceso de generación de planillas falló, contacte al adminstrador.";
                response.Encabezado = "Error";
                response.Tipo = "error";
            }
            //retornar resultado al cliente
            return Json(new { Data = reporte, Response = response }, JsonRequestBehavior.AllowGet);
        }

        private static decimal SalarioPromedioAnualISR(decimal? netoAPagarColaborador, ref decimal SueldoAnualISR, List<decimal> mesesPago, bool esMensual, ref decimal SalarioPromedioAnualPagadoAlAnio, ref decimal salarioPromedioAnualPagadoAlMes)
        {
            if (esMensual)
            {
                //Si es el primer mes a cobrar
                if (mesesPago == null)
                    SueldoAnualISR = ((netoAPagarColaborador * 12) / 12) ?? 0;

                int cantidadMesesPagados = mesesPago.Count;

                decimal promedioMesesPago = mesesPago.Average();

                decimal sueldoProyeccion = 0;

                //Sacar el sueldo de los meses restantes
                for (int i = cantidadMesesPagados; i <= 12; i++)
                {
                    sueldoProyeccion += promedioMesesPago;
                }

                salarioPromedioAnualPagadoAlMes = mesesPago.Sum() + sueldoProyeccion;
            }
            else
            {
                if (DateTime.Now.Month == 12)
                    //Calcular todas las fechas de este año, aunque haya entrado 
                    SalarioPromedioAnualPagadoAlAnio = mesesPago.Sum();
            }

            return (salarioPromedioAnualPagadoAlMes > 0) ? salarioPromedioAnualPagadoAlMes : SalarioPromedioAnualPagadoAlAnio;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    class iziToast
    {
        public string Response { get; set; }
        public string Encabezado { get; set; }
        public string Tipo { get; set; }
    }
}