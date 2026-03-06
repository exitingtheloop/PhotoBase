/* gulpfile.js — USWDS v3 compile pipeline
 *
 * Mirrors the original v2 directory structure:
 *   wwwroot/uswds-custom/dist/   ? compiled CSS
 *   wwwroot/uswds-custom/fonts/? fonts (../fonts/ from dist/)
 *   wwwroot/uswds-custom/img/    ? images (../img/ from dist/)
 *wwwroot/uswds/js/     ? JS (unchanged location)
 *
 * This ensures all relative url() paths in compiled CSS resolve correctly.
 */

const uswds = require("@uswds/compile");

uswds.settings.version = 3;

uswds.paths.dist.css   = "./wwwroot/uswds-custom/dist";
uswds.paths.dist.fonts = "./wwwroot/uswds-custom/fonts";
uswds.paths.dist.img   = "./wwwroot/uswds-custom/img";
uswds.paths.dist.js    = "./wwwroot/uswds/js";
uswds.paths.dist.theme = "./scss/uswds";

uswds.paths.src.projectSass = "./scss/uswds";

exports.init     = uswds.init;
exports.compile  = uswds.compile;
exports.watch    = uswds.watch;
exports.update   = uswds.updateUswds;
exports.copyAll  = uswds.copyAll;
exports.default  = uswds.compile;
