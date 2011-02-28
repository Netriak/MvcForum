/// <reference path="jquery-1.4.4-vsdoc.js"/>
/// <reference path="jquery.markitup.js"/>
mySettings = {
    onTab: { keepDefault: false, replaceWith: '\t' },
    previewParserPath: '/Preview',
    previewAutoRefresh: true,
    markupSet: [
		{ name: 'Bold', key: 'B', openWith: '[b]', closeWith: '[/b]' },
		{ name: 'Italic', key: 'I', openWith: '[i]', closeWith: '[/i]' },
		{ name: 'Stroke through', key: 'S', openWith: '[s]', closeWith: '[/s]' },
        { name: 'Underline', key: 'U', openWith: '[u]', closeWith: '[/u]' },
		{ separator: '---------------' },
		{ name: 'Picture', key: 'P', replaceWith: '[img][![Source:!:http://]!][/img]' },
		{ name: 'Link', key: 'L', openWith: '[url=[![Link:!:http://]!]]', closeWith: '[/url]', placeHolder: 'Your text to link...' },
        { separator: '---------------' },
        { name: 'Align: Center', openWith: '[center]', closeWith: '[/center]' },
        { name: 'Align: Justify', openWith: '[justify]', closeWith: '[/justify]' },
        { name: 'Align: Left', openWith: '[left]', closeWith: '[/left]' },
        { name: 'Align: Right', openWith: '[right]', closeWith: '[/right]' },
        { separator: '---------------' },
        { name: 'Code', openWith: '[code]', closeWith: '[/code]' },
        { name: 'Spoiler', openWith: '[spoiler]', closeWith: '[/spoiler]' },
        { name: 'Youtube video', replaceWith: '[youtube][![Youtube video ID:!:]!][/youtube]' }
	]
}

$(document).ready(function () {
    // $(".spoiler>a").click(togglespoiler); // BBCode spoiler tag

    $('.spoiler').siblings("div").addClass("spoilertext").delegate('a', 'click', togglespoiler);

    $("#Username,#Password").focus(ClearDefaultValue).blur(RestoreDefaultValue);

    if ($.markItUp) {
        $("#PostText").markItUp(mySettings); // MarkItUp post editor
        $("#PreviewButton").click(MarkItUpPreview);
    }
});

function MarkItUpPreview() {
    $('textarea').trigger('preview');
    return false;
}

function togglespoiler() {
    $(this).siblings('div').slideToggle(500);
}

function RestoreDefaultValue() {
    var t = $(this);
    if (t.val() == "") {
        t.val(t.attr("name"));
    }
}

function ClearDefaultValue() {
    var t = $(this);
    if (t.val() == t.attr("name")) {
        t.val("");
    }
}