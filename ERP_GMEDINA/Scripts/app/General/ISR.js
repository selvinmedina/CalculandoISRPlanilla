﻿////FUNCION GENERICA PARA REUTILIZAR AJAX
function _ajax(params, uri, type, callback) {
    $.ajax({
        url: uri,
        type: type,
        data: { params },
        success: function (data) {
            callback(data);
        }
    });
}
var InactivarID = 0;

//OBTENER SCRIPT DE FORMATEO DE FECHA
$.getScript("../Scripts/app/General/SerializeDate.js")
  .done(function (script, textStatus) {
      console.log(textStatus);
  })
  .fail(function (jqxhr, settings, exception) {
      console.log("No se pudo recuperar Script SerializeDate");
  });

// EVITAR POSTBACK DE FORMULARIOS 
$("#frmEditISR").submit(function (e) {
    return false;
});
$("#frmISRCreate").submit(function (e) {
    return false;
});

//FUNCION: CARGAR DATA Y REFRESCAR LA TABLA DEL INDEX
function cargarGridISR() {
    _ajax(null,
        '/ISR/GetData',
        'GET',
        (data) => {
            if (data.length == 0) {
                iziToast.error({
                    title: 'Error',
                    message: 'No se pudo cargar la información, contacte al administrador',
                });
            }
            var ListaISR = data, template = '';
            //RECORRER DATA OBETINA Y CREAR UN "TEMPLATE" PARA REFRESCAR EL TBODY DE LA TABLA DEL INDEX
            for (var i = 0; i < ListaISR.length; i++) {
                template += '<tr data-id = "' + ListaISR[i].isr_Id + '">' +
                    '<td>' + ListaISR[i].isr_RangoInicial + '</td>' +
                    '<td>' + ListaISR[i].isr_RangoFinal + '</td>' +
                    '<td>' + ListaISR[i].isr_Porcentaje + '</td>' +
                    '<td>' + ListaISR[i].tde_Descripcion + '</td>' +
                    '<td>' +
                    '<button data-id = "' + ListaISR[i].isr_Id + '" type="button" class="btn btn-primary btn-xs"  id="btnModalEditarISR">Editar</button>' +
                    '<button data-id = "' + ListaISR[i].isr_Id + '" type="button" class="btn btn-default btn-xs"  id="btnDetalleISR">Detalle</button>' +
                    '</td>' +
                    '</tr>';
            }
            //REFRESCAR EL TBODY DE LA TABLA DEL INDEX
            $('#tbodyISR').html(template);
        });
    FullBody();
}

//Modal Create ISR
$(document).on("click", "#btnAgregarISR", function () {
    //PEDIR DATA PARA LLENAR EL DROPDOWNLIST DEL MODAL
    $.ajax({
        url: "/ISR/EditGetDDL",
        method: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8"
    })
        //LLENAR EL DROPDONWLIST DEL MODAL CON LA DATA OBTENIDA
        .done(function (data) {
            $("#Crear #tde_IdTipoDedu").empty();
            $("#Crear #tde_IdTipoDedu").append("<option value='0'>Selecione una opción...</option>");
            $.each(data, function (i, iter) {
                $("#Crear #tde_IdTipoDedu").append("<option value='" + iter.Id + "'>" + iter.Descripcion + "</option>");
            });
        });
    //MOSTRAR EL MODAL DE AGREGAR
    $(".field-validation-error").css('display', 'none');
    $('#Crear input[type=text], input[type=number]').val('');
    $("#AgregarISR").modal();
});

//FUNCION: CREAR EL NUEVO REGISTROISR
$('#btnCreateISR').click(function () {
    var ModelState = true;

    //$("#Editar #tede_Id").val() == "" ? ModelState = false : '';
    $("#Crear #isr_RangoInicial").val() == "" ? ModelState = false : $("#Crear #isr_RangoInicial").val() == "0.00" ? ModelState = false : $("#Crear #isr_RangoInicial").val() == null ? ModelState = false : isNaN($("#Crear #isr_RangoInicial").val()) == true ? ModelState = false : '';
    $("#Crear #isr_RangoFinal").val() == "" ? ModelState = false : $("#Crear #isr_RangoFinal").val() == "0.00" ? ModelState = false : $("#Crear #isr_RangoFinal").val() == null ? ModelState = false : isNaN($("#Crear #isr_RangoFinal").val()) == true ? ModelState = false : '';
    $("#Crear #isr_Porcentaje").val() == "" ? ModelState = false : $("#Crear #isr_Porcentaje").val() == "0" ? ModelState = false : $("#Crear #isr_Porcentaje").val() == null ? ModelState = false : isNaN($("#Crear #isr_Porcentaje").val()) == true ? ModelState = false : '';
    $("#Crear #tde_IdTipoDedu").val() == "" ? ModelState = false : $("#Crear #tde_IdTipoDedu").val() == "0" ? ModelState = false : $("#Crear #tde_IdTipoDedu").val() == null ? ModelState = false : isNaN($("#Crear #tde_IdTipoDedu").val()) == true ? ModelState = false : '';

    //SERIALIZAR EL FORMULARIO DEL MODAL (ESTÁ EN LA VISTA PARCIAL)
    if (ModelState) {
        var data = $("#frmISRCreate").serializeArray();
        $.ajax({
            url: "/ISR/Create",
            method: "POST",
            data: data
        }).done(function (data) {
            $("#AgregarISR").modal('hide');
            //VALIDAR RESPUESTA OBETNIDA DEL SERVIDOR, SI LA INSERCIÓN FUE EXITOSA O HUBO ALGÚN ERROR
            if (data == "error") {
                iziToast.error({
                    title: 'Error',
                    message: 'No se pudo guardar el registro, contacte al administrador',
                });
            }
            else if (data == "bien") {
                cargarGridISR();
                console.log(data);
                // Mensaje de exito cuando un registro se ha guardado bien
                iziToast.success({
                    title: 'Exito',
                    message: '¡El registro fue agregado de forma exitosa!',
                });
            }
        });
    }
    else {
        iziToast.error({
            title: 'Error',
            message: 'Ingrese datos válidos.',
        });
    }

});

//FUNCION: PRIMERA FASE DE EDICION DE REGISTROS, MOSTRAR MODAL CON LA INFORMACIÓN DEL REGISTRO SELECCIONADO
$(document).on("click", "#tblISR tbody tr td #btnModalEditarISR", function () {
    var ID = $(this).data('id');
    $("#EditISR").modal('show');
    InactivarID = ID;
    $.ajax({
        url: "/ISR/Edit/" + ID,
        method: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ ID: ID })
    })
        .done(function (data) {
            //SI SE OBTIENE DATA, LLENAR LOS CAMPOS DEL MODAL CON ELLA
            if (data) {
                $("#Editar #isr_Id").val(data.isr_Id);
                $("#Editar #isr_RangoInicial").val(data.isr_RangoInicial);
                $("#Editar #isr_RangoFinal").val(data.isr_RangoFinal);
                $("#Editar #isr_Porcentaje").val(data.isr_Porcentaje);
                $("#Editar #tde_IdTipoDedu").val(data.tde_IdTipoDedu);
                $(".field-validation-error").css('display', 'none');
                $("#EditarISR").modal();
                //GUARDAR EL ID DEL DROPDOWNLIST (QUE ESTA EN EL REGISTRO SELECCIONADO) QUE NECESITAREMOS PONER SELECTED EN EL DDL DEL MODAL DE EDICION
                var SelectedId = data.tde_IdTipoDedu;
                //CARGAR INFORMACIÓN DEL DROPDOWNLIST PARA EL MODAL
                $.ajax({
                    url: "/ISR/EditGetDDL",
                    method: "GET",
                    dataType: "json",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ ID })
                })
                    .done(function (data) {
                        //LIMPIAR EL DROPDOWNLIST ANTES DE VOLVER A LLENARLO
                        $("#Editar #tde_IdTipoDedu").empty();
                        //LLENAR EL DROPDOWNLIST
                        $("#Editar #tde_IdTipoDedu").append("<option value=0>Selecione una opción...</option>");
                        $.each(data, function (i, iter) {
                            $("#Editar #tde_IdTipoDedu").append("<option" + (iter.Id == SelectedId ? " selected" : " ") + " value='" + iter.Id + "'>" + iter.Descripcion + "</option>");
                        });
                    });
            }
            else {
                //Mensaje de error si no hay data
                iziToast.error({
                    title: 'Error',
                    message: 'No se pudo cargar la información, contacte al administrador',
                });
            }
        });
});

//EJECUTAR EDICIÓN DEL REGISTRO EN EL MODAL
$("#btnEditarISR").click(function () {

    var ModelState = true;
    $("#Editar #isr_Id").val() == "" ? ModelState = false : $("#Editar #isr_Id").val() == "0" ? ModelState = false : $("#Editar #isr_Id").val() == null ? ModelState = false : '';
    $("#Editar #isr_RangoInicial").val() == "" ? ModelState = false : $("#Editar #isr_RangoInicial").val() == "0.00" ? ModelState = false : $("#Editar #isr_RangoInicial").val() == null ? ModelState = false : '';
    $("#Editar #isr_RangoFinal").val() == "" ? ModelState = false : $("#Editar #isr_RangoFinal").val() == "0.00" ? ModelState = false : $("#Editar #isr_RangoFinal").val() == null ? ModelState = false : '';
    $("#Editar #isr_Porcentaje").val() == "" ? ModelState = false : $("#Editar #isr_Porcentaje").val() == "0" ? ModelState = false : $("#Editar #isr_Porcentaje").val() == null ? ModelState = false : '';
    $("#Editar #tde_IdTipoDedu").val() == "" ? ModelState = false : $("#Editar #tde_IdTipoDedu").val() == "0" ? ModelState = false : $("#Editar #tde_IdTipoDedu").val() == null ? ModelState = false : '';

    if (ModelState) {
        //SERIALIZAR EL FORMULARIO (QUE ESTÁ EN LA VISTA PARCIAL) DEL MODAL, SE PARSEA A FORMATO JSON
        var data = $("#frmEditISR").serializeArray();
        //SE ENVIA EL JSON AL SERVIDOR PARA EJECUTAR LA EDICIÓN
        $.ajax({
            url: "/ISR/Edit",
            method: "POST",
            data: data
        }).done(function (data) {
            if (data == "error") {
                //Cuando traiga un error del backend al guardar la edicion
                iziToast.error({
                    title: 'Error',
                    message: 'No se pudo editar el registro, contacte al administrador',
                });
            }
            else {
                cargarGridISR();
                //UNA VEZ REFRESCADA LA TABLA, SE OCULTA EL MODAL
                $("#EditarISR").modal('hide');
                //Mensaje de exito de la edicion
                iziToast.success({
                    title: 'Éxito',
                    message: '¡El registro fue editado de forma exitosa!',
                });
            }
        });
    }
    else {
        iziToast.error({
            title: 'Error',
            message: 'Ingrese datos válidos.',
        });
    }
});

//FUNCION: OCULTAR MODAL DE EDICIÓN
$("#btnCerrarEditar").click(function () {
    $("#EditarISR").modal('hide');
});




$(document).on("click", "#btnModalInactivarISR", function () {
    $("#EditarISR").modal('hide');
    $("#InactivarISR").modal();
});



//Inactivar registro Techos Deducciones    
$("#btnInactivarISR").click(function () {
    var data = $("#frmInactivarISR").serializeArray();
    //SE ENVIA EL JSON AL SERVIDOR PARA EJECUTAR LA EDICIÓN
    $.ajax({
        url: "/ISR/Inactivar/" + InactivarID,
        method: "POST",
        data: data
    }).done(function (data) {
        if (data == "error") {
            //Cuando traiga un error del backend al guardar la edicion
            iziToast.error({
                title: 'Error',
                message: 'No se pudo inactivar el registro, contacte al administrador',
            });
        }
        else {
            cargarGridISR();
            //UNA VEZ REFRESCADA LA TABLA, SE OCULTA EL MODAL
            $("#InactivaISR").modal('hide');
            //Mensaje de exito de la edicion
            iziToast.success({
                title: 'Éxito',
                message: '¡El registro fue Inactivado de forma exitosa!',
            });
        }
    });
    InactivarID = 0;
});


//DETALLES
$(document).on("click", "#tblISR tbody tr td #btnDetalleISR", function () {
    var ID = $(this).data('id');
    $.ajax({
        url: "/ISR/Details/" + ID,
        method: "GET",
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify({ ID: ID })
    })
        .done(function (data) {
            //SI SE OBTIENE DATA, LLENAR LOS CAMPOS DEL MODAL CON ELLA
            if (data) {
                var FechaCrea = FechaFormato(data[0].isr_FechaCrea);
                var FechaModifica = FechaFormato(data[0].isr_FechaModifica);
                $("#Detalles #isr_Id").val(data[0].isr_Id);
                $("#Detalles #isr_RangoInicial").val(data[0].isr_RangoInicial);
                $("#Detalles #isr_RangoFinal").val(data[0].isr_RangoFinal);
                $("#Detalles #isr_Porcentaje").val(data[0].isr_Porcentaje);
                $("#Detalles #tde_IdTipoDedu").val(data[0].tde_IdTipoDedu);
                $("#Detalles #isr_UsuarioCrea").val(data[0].isr_UsuarioCrea);
                $("#Detalles #tbUsuario_usu_NombreUsuario").val(data[0].UsuCrea);
                $("#Detalles #isr_FechaCrea").val(FechaCrea);
                $("#Detalles #isr_UsuarioModifica").val(data.isr_UsuarioModifica);
                data[0].UsuModifica == null ? $("#Detalles #tbUsuario1_usu_NombreUsuario").val('Sin modificaciones') : $("#Detalles #tbUsuario1_usu_NombreUsuario").val(data[0].UsuModifica);
                $("#Detalles #isr_FechaModifica").val(FechaModifica);
                //GUARDAR EL ID DEL DROPDOWNLIST (QUE ESTA EN EL REGISTRO SELECCIONADO) QUE NECESITAREMOS PONER SELECTED EN EL DDL DEL MODAL DE EDICION
                var SelectedId = data[0].tde_IdTipoDedu;
                //CARGAR INFORMACIÓN DEL DROPDOWNLIST PARA EL MODAL
                $.ajax({
                    url: "/ISR/EditGetDDL",
                    method: "GET",
                    dataType: "json",
                    contentType: "application/json; charset=utf-8",
                    data: JSON.stringify({ ID })
                })
                    .done(function (data) {
                        //LIMPIAR EL DROPDOWNLIST ANTES DE VOLVER A LLENARLO
                        $("#Detalles #tde_IdTipoDedu").empty();
                        //LLENAR EL DROPDOWNLIST
                        $("#Detalles #tde_IdTipoDedu").append("<option value=0>Selecione una opción...</option>");
                        $.each(data, function (i, iter) {
                            $("#Detalles #tde_IdTipoDedu").append("<option" + (iter.Id == SelectedId ? " selected" : " ") + " value='" + iter.Id + "'>" + iter.Descripcion + "</option>");
                        });
                    });
                $("#DetailsISR").modal();
            }
            else {
                //Mensaje de error si no hay data
                iziToast.error({
                    title: 'Error',
                    message: 'No se pudo cargar la información, contacte al administrador',
                });
            }
        });
});