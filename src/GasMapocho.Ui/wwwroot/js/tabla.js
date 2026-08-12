/* ============================================================
   tabla.js — búsqueda + paginación genérica en cliente.
   Se escribe UNA vez y la usan los 3 mantenimientos y los 2
   reportes. Cuando exista el backend, el filtrado pasa a ser
   por Ajax; la interfaz de uso no cambia.
   ============================================================ */
window.GasApp = window.GasApp || {};

GasApp.tabla = (function ($) {
  'use strict';

  function init(contenedor) {
    var $c = $(contenedor);
    // Las filas de detalle desplegable no son filas de datos: no se cuentan
    // ni se paginan, siguen a la suya.
    var $filas = $c.find('tbody tr').not('.detalle-fila');
    var $detalles = $c.find('tbody tr.detalle-fila');
    var $buscador = $c.find('.js-buscar');
    var $info = $c.find('.js-info');
    var $pag = $c.find('.js-paginacion');
    var porPagina = parseInt($c.data('por-pagina'), 10) || 5;
    var buscarEnServidor = $c.data('buscar-servidor') === 'true';
    var pagina = 1;
    var termino = '';

    function coincide($fila) {
      if (!termino) { return true; }
      return $fila.text().toLowerCase().indexOf(termino) !== -1;
    }

    function render() {
      var $visibles = $filas.filter(function () { return coincide($(this)); });
      var total = $visibles.length;
      var totalPaginas = Math.max(1, Math.ceil(total / porPagina));
      if (pagina > totalPaginas) { pagina = totalPaginas; }

      var desde = (pagina - 1) * porPagina;
      var hasta = desde + porPagina;

      $filas.hide();
      // Al ocultar una fila se cierra su detalle y se restablece el botón
      $detalles.hide().prop('hidden', true);
      $c.find('.js-detalle').attr('aria-expanded', 'false')
        .find('.material-symbols-outlined').text('expand_more');

      $visibles.slice(desde, hasta).show();

      $c.find('.js-vacio').toggle(total === 0);
      $c.find('.tabla-gs-wrap').toggle(total > 0);

      if ($info.length) {
        $info.text(total === 0
          ? 'Sin resultados'
          : 'Mostrando ' + (desde + 1) + ' a ' + Math.min(hasta, total) + ' de ' + total);
      }

      renderPaginacion(totalPaginas);
    }

    function renderPaginacion(totalPaginas) {
      if (!$pag.length) { return; }
      var html = '';
      html += '<a href="#" class="pag js-pag' + (pagina === 1 ? ' deshabilitada' : '') +
              '" data-p="' + (pagina - 1) + '">Anterior</a>';
      for (var i = 1; i <= totalPaginas; i++) {
        html += '<a href="#" class="pag js-pag' + (i === pagina ? ' activa' : '') +
                '" data-p="' + i + '"' + (i === pagina ? ' aria-current="page"' : '') + '>' + i + '</a>';
      }
      html += '<a href="#" class="pag js-pag' + (pagina === totalPaginas ? ' deshabilitada' : '') +
              '" data-p="' + (pagina + 1) + '">Siguiente</a>';
      $pag.html(html);
    }

    // debounce de 300 ms: no filtra en cada tecla
    var t;
    $buscador.on('input', function () {
      var v = $(this).val();
      clearTimeout(t);

      if (buscarEnServidor) {
        // El filtrado en cliente solo ve las filas ya cargadas: para que el
        // buscador encuentre cualquier registro, se recarga con ?busqueda=
        // y es la API (SQL) la que filtra sobre todo el universo de datos.
        t = setTimeout(function () {
          var url = new URL(window.location.href);
          if (v) { url.searchParams.set('busqueda', v); }
          else { url.searchParams.delete('busqueda'); }
          window.location.href = url.toString();
        }, 300);
        return;
      }

      var vLower = v.toLowerCase();
      t = setTimeout(function () { termino = vLower; pagina = 1; render(); }, 300);
    });

    $pag.on('click', '.js-pag', function (e) {
      e.preventDefault();
      var p = parseInt($(this).data('p'), 10);
      if (p >= 1) { pagina = p; render(); }
    });

    render();
  }

  // Detalle desplegable. Vive aquí porque lo usan el historial del cliente
  // y el listado de pedidos del panel: una vista menos en cada uno.
  function initDetalles() {
    $(document).on('click', '.js-detalle', function () {
      var $btn = $(this);
      var $det = $('#' + $btn.attr('aria-controls'));
      var abierto = $btn.attr('aria-expanded') === 'true';

      $btn.attr('aria-expanded', String(!abierto));
      $btn.find('.material-symbols-outlined').text(abierto ? 'expand_more' : 'expand_less');
      $det.prop('hidden', abierto).toggle(!abierto);
    });
  }

  return {
    init: function () {
      $('.js-tabla').each(function () { init(this); });
      initDetalles();
    }
  };
})(jQuery);

jQuery(function () { GasApp.tabla.init(); });
