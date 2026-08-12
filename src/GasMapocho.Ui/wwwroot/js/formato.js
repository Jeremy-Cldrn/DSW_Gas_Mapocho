/* ============================================================
   Formato — Gas Mapocho
   El peso chileno NO tiene decimales: $14.500, nunca $14.500,00.
   El punto separa miles (al revés que en inglés).
   ============================================================ */
window.GasApp = window.GasApp || {};

GasApp.formato = (function () {
  'use strict';

  var clp = new Intl.NumberFormat('es-CL', {
    style: 'currency',
    currency: 'CLP',
    maximumFractionDigits: 0
  });

  var fecha = new Intl.DateTimeFormat('es-CL', {
    day: '2-digit', month: '2-digit', year: 'numeric'
  });

  return {
    // 14500 -> "$14.500"
    moneda: function (valor) { return clp.format(Number(valor) || 0); },
    fecha: function (valor) { return fecha.format(new Date(valor)); }
  };
})();

/* ------------------------------------------------------------
   Trampa de jquery.validate con es-CL:
   interpreta el punto como separador decimal, de modo que
   "14.500" se validaría como 14,5 y se guardaría mal SIN ERROR.

   Los campos de precio y stock usan type="number" (solo dígitos),
   pero se corrige igual por si aparece un input de texto.
   ------------------------------------------------------------ */
(function ($) {
  if (!$ || !$.validator) { return; }

  $.validator.methods.number = function (value, element) {
    return this.optional(element) ||
      /^-?(?:\d+|\d{1,3}(?:\.\d{3})+)(?:,\d+)?$/.test(value);
  };

  $.validator.methods.range = function (value, element, param) {
    var n = parseFloat(String(value).replace(/\./g, '').replace(',', '.'));
    return this.optional(element) || (n >= param[0] && n <= param[1]);
  };
})(window.jQuery);
