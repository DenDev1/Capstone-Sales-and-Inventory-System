window.setTimeout(function () {

    $(".alert").fadeTo(500, 0).slideUp(500, function () {
        $(this).remove();
    });
}, 3000);

$(document).ready(function () {
    $("#open").click(function () {
        $("#open").hide();
        $("#closed").show();
        $("#password").attr("type", "text");
    });

    $("#closed").click(function () {
        $("#closed").hide();
        $("#open").show();
        $("#password").attr("type", "password");
    });
});
//function loader() {
//    document.querySelector('.loader-container').classList.add('fade-out');
//}

//function fadeOut() {
//    setInterval(loader, 5000);
//}

//window.onload = fadeOut;
// In your Javascript (external .js resource or <script> tag)
$(document).ready(function () {
    $('#example').select2(); // Initialize Select2
});
