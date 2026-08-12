/* ============================================================
   Catálogo: búsqueda con debounce y agregar al carrito por Ajax.
   Los selectores js- no se usan para dar estilo, así un cambio de
   diseño no rompe el comportamiento.
   ============================================================ */
(function ($) {
  'use strict';

  var $grilla = $('#grilla');
  var $avisos = $('#avisos');

  /* --- Aviso flotante --- */
  function aviso(texto, tipo) {
    var $t = $('<div class="toast-gs"><div class="cuerpo"></div></div>');
    if (tipo === 'error') { $t.css('border-left-color', 'var(--gs-red-500)'); }
    $t.find('.cuerpo').text(texto);
    $avisos.append($t);
    setTimeout(function () { $t.fadeOut(200, function () { $t.remove(); }); }, 3500);
  }

  /* --- Selector de cantidad (delegado: sobrevive al reemplazo de la grilla) --- */
  $grilla.on('click', '.js-mas', function () {
    var $i = $(this).siblings('.js-cantidad');
    var max = parseInt($i.attr('max'), 10) || 99;
    $i.val(Math.min(max, (parseInt($i.val(), 10) || 1) + 1));
  });

  $grilla.on('click', '.js-menos', function () {
    var $i = $(this).siblings('.js-cantidad');
    $i.val(Math.max(1, (parseInt($i.val(), 10) || 1) - 1));
  });

  /* --- Agregar al carrito --- */
  $grilla.on('click', '.js-agregar', function () {
    var $btn = $(this);
    var $card = $btn.closest('.producto-card');
    var cantidad = parseInt($card.find('.js-cantidad').val(), 10) || 1;
    var textoOriginal = $btn.text();

    $btn.prop('disabled', true).text('Agregando…');

    $.post('/Carrito/Agregar', { idProducto: $btn.data('id'), cantidad: cantidad })
      .done(function (r) {
        if (r.ok) {
          $('.js-carrito-contador').text(r.items).removeClass('vacio');
          aviso(r.mensaje);
        } else {
          aviso(r.mensaje, 'error');
        }
      })
      .fail(function () { aviso('No pudimos agregar el producto. Intenta de nuevo.', 'error'); })
      // El botón SIEMPRE se restablece, aunque la llamada falle
      .always(function () { $btn.prop('disabled', false).text(textoOriginal); });
  });

})(jQuery);
