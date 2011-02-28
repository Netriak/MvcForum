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
        { name: 'Center', openWith: '[center]', closeWith: '[/center]' },
        { name: 'Justify', openWith: '[justify]', closeWith: '[/justify]' },
        { name: 'Left', openWith: '[left]', closeWith: '[/left]' },
        { name: 'Right', openWith: '[right]', closeWith: '[/right]' },
	]
}

$(document).ready(function () {
    $(".spoiler>a").click(togglespoiler).siblings("div").addClass("spoilertext"); // BBCode spoiler tag
    $("#PostText").markItUp(mySettings); // MarkItUp post editor
    $("#PreviewButton").click(MarkItUpPreview);
});

function MarkItUpPreview() {
    $('textarea').trigger('preview');
    return false;
}

function togglespoiler() {
    $(this).siblings('div').slideToggle("fast");
}
