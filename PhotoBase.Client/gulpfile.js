/* gulpfile.js — USWDS compile pipeline
 * Official setup per:
 *   https://designsystem.digital.gov/documentation/getting-started/developers/phase-two-compile/
 *
 * Paths configured to output into wwwroot/ for Blazor WASM static serving.
 */

const uswds = require("@uswds/compile");

/**
 * USWDS version — tells @uswds/compile which package to look for.
 * 3 = @uswds/uswds (v3.x)
 */
uswds.settings.version = 3;

/**
 * Path settings
 * Per official docs, these tell the compiler where to find and place things.
 */

// Where to put compiled CSS, JS, fonts, images (Blazor serves from wwwroot)
uswds.paths.dist.css  = "./wwwroot/css/uswds";
uswds.paths.dist.js     = "./wwwroot/js/uswds";
uswds.paths.dist.fonts  = "./wwwroot/fonts";
uswds.paths.dist.img    = "./wwwroot/img";
uswds.paths.dist.theme  = "./scss/uswds";

// Where custom SCSS overrides live (Phase 3 customization)
uswds.paths.src.projectSass = "./scss/uswds";

/**
 * Exports — register the official USWDS gulp tasks.
 *
 * Available tasks after this:
 *   npx gulp init     — first-time setup: copies theme files + compiles
 *   npx gulp compile  — recompile SCSS only
 *   npx gulp watch    — watch for changes and recompile
 *   npx gulp copyAll  — copy fonts, images, JS from USWDS package
 *   npx gulp default  — same as compile
 */
exports.init      = uswds.init;
exports.compile   = uswds.compile;
exports.watch     = uswds.watch;
exports.update    = uswds.updateUswds;
exports.copyFonts = uswds.copyFonts;
exports.copyImages = uswds.copyImages;
exports.copyJS    = uswds.copyJS;
exports.copyAll= uswds.copyAll;
exports.default   = uswds.compile;
