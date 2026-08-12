/* ============================================================
   Carrito: cambiar cantidad y quitar líneas sin recargar.
   Los totales los calcula el servidor y llegan ya formateados
   en CLP, para no duplicar la lógica de formato.
   ============================================================ */
(function ($) {
  'use strict';

  var $avisos = $('#avisos');

  function aviso(texto, tipo) {
    var $t = $('<div class="toast-gs"><div class="cuerpo"></div></div>');
    if (tipo === 'error') { $t.css('border-left-color', 'var(--gs-red-500)'); }
    $t.find('.cuerpo').text(texto);
    $avisos.append($t);
    setTimeout(function () { $t.fadeOut(200, function () { $t.remove(); }); }, 3500);
  }

  function pintarTotales(r) {
    $('.js-subtotal').text(r.subtotalTexto);
    $('.js-despacho').text(r.despachoTexto);
    $('.js-total').text(r.totalTexto);
    $('.js-carrito-contador').text(r.items).toggleClass('vacio', r.items === 0);

    $.each(r.lineas, function (_, l) {
      $('tr[data-id="' + l.IdProducto + '"] .js-sub').text(l.subtotalTexto);
    });

    // Al vaciarse, se recarga para mostrar el estado vacío
    if (r.vacio) { location.reload(); }
  }

  function actualizar($fila, cantidad) {
    var id = $fila.data('id');
    $fila.css('opacity', .5);

    $.post('/Carrito/Actualizar', { idProducto: id, cantidad: cantidad })
      .done(function (r) {
        if (r.ok) {
          if (cantidad === 0) { $fila.remove(); }
          pintarTotales(r);
        } else {
          aviso(r.mensaje, 'error');
          location.reload();   // el servidor manda: se resincroniza
        }
      })
      .fail(function () { aviso('No pudimos actualizar el carrito.', 'error'); })
      .always(function () { $fila.css('opacity', 1); });
  }

  $('.tabla-gs').on('click', '.js-mas', function () {
    var $f = $(this).closest('tr');
    var $i = $f.find('.js-cant');
    var v = (parseInt($i.val(), 10) || 0) + 1;
    $i.val(v);
    actualizar($f, v);
  });

  $('.tabla-gs').on('click', '.js-menos', function () {
    var $f = $(this).closest('tr');
    var $i = $f.find('.js-cant');
    var v = Math.max(0, (parseInt($i.val(), 10) || 0) - 1);
    $i.val(v);
    actualizar($f, v);
  });

  $('.tabla-gs').on('change', '.js-cant', function () {
    var $f = $(this).closest('tr');
    actualizar($f, Math.max(0, parseInt($(this).val(), 10) || 0));
  });

  $('.tabla-gs').on('click', '.js-quitar', function () {
    var $f = $(this).closest('tr');
    $.post('/Carrito/Quitar', { idProducto: $f.data('id') })
      .done(function (r) {
        $f.remove();
        pintarTotales(r);
        aviso(r.mensaje);
      })
      .fail(function () { aviso('No pudimos quitar el producto.', 'error'); });
  });

})(jQuery);
