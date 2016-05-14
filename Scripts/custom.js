$(function () {
    $('#isStores').change(function () {

        if ($("#isStores").attr("checked")) {
            $('.stores').show();
            value = 1;
        } else {
            $('.stores').hide();
            value = 1;
        }
    });
});



$(document).ready(function () {

    $('.stores').hide();

});

