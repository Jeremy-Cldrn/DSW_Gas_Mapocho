/* ============================================================
   Panel: sidebar off-canvas, avisos y simulación de SignalR.
   ============================================================ */
window.GasApp = window.GasApp || {};

(function ($) {
  'use strict';

  /* --- Sidebar off-canvas --- */
  var $sidebar = $('#sidebar');
  var $backdrop = $('#backdrop');
  var $btnMenu = $('#btn-menu');

  function abrir(v) {
    $sidebar.toggleClass('abierto', v);
    $backdrop.prop('hidden', !v);
    $btnMenu.attr('aria-expanded', String(v));
  }

  $btnMenu.on('click', function () { abrir(!$sidebar.hasClass('abierto')); });
  $backdrop.on('click', function () { abrir(false); });
  $(document).on('keydown', function (e) { if (e.key === 'Escape') { abrir(false); } });

  /* --- Avisos flotantes --- */
  GasApp.toast = {
    mostrar: function (o) {
      var $t = $('<div class="toast-gs"></div>');
      $t.append($('<div class="titulo"></div>').text(o.titulo || 'Aviso'));
      $t.append($('<div class="cuerpo"></div>').text(o.cuerpo || ''));
      if (o.enlace) {
        $t.append($('<a class="d-inline-block mt-2" style="font-size:var(--fs-sm)"></a>')
          .attr('href', o.enlace).text('Ver pedido'));
      }
      if (o.tipo === 'error') { $t.css('border-left-color', 'var(--gs-red-500)'); }
      $('#avisos').append($t);
      setTimeout(function () { $t.fadeOut(200, function () { $t.remove(); }); }, o.duracion || 8000);
    }
  };

  /* --- Estado de la conexión --- */
  GasApp.conexion = function (estado) {
    var $c = $('#conexion');
    $c.removeClass('reconectando caido');
    if (estado === 'reconectando') { $c.addClass('reconectando').find('.texto').text('Reconectando…'); }
    else if (estado === 'caido')   { $c.addClass('caido').find('.texto').text('Sin conexión'); }
    else                           { $c.find('.texto').text('Conectado'); }
  };

  /* --- Confirmación antes de una acción irreversible ---
     Modal propio en vez de window.confirm(): el encargo pide un diálogo con
     Aceptar/Cancelar, no el cuadro nativo del navegador. El modal vive una
     sola vez en _LayoutAdmin; aquí solo se le cambia el texto y, al aceptar,
     se envía el <form> que contenía el botón que se apretó. */
  var $modalConfirmar = $('#modal-confirmar');
  var $formPendiente = null;

  $(document).on('click', '.js-confirmar', function (e) {
    e.preventDefault();
    e.stopImmediatePropagation();

    var $form = $(this).closest('form');

    // form.submit() (más abajo) no dispara el evento "submit", así que
    // jQuery Validate nunca se enteraría de un campo vacío. Si el formulario
    // tiene validación (el de editar producto/cliente la trae), se revisa
    // acá: con datos inválidos se muestran esos errores, no el modal.
    if ($form.length && typeof $form.valid === 'function' && !$form.valid()) { return; }

    $formPendiente = $form;
    $modalConfirmar.find('.js-modal-confirmar-texto').text($(this).data('confirmar') || '¿Confirmas esta acción?');
    bootstrap.Modal.getOrCreateInstance($modalConfirmar[0]).show();
  });

  $modalConfirmar.find('.js-modal-confirmar-aceptar').on('click', function () {
    bootstrap.Modal.getOrCreateInstance($modalConfirmar[0]).hide();
    if ($formPendiente && $formPendiente.length) { $formPendiente[0].submit(); }
    $formPendiente = null;
  });

})(jQuery);
