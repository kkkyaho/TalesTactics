#pragma warning disable CS0162
// Editor eval only. Execute in the already-running Play session; this script does not start/stop it.
// Read-only scene/asset inspection. The only write is a new JSON under Logs; refuses to overwrite.
// References: craft-game-floors/references/visual-scorecard.md; Unity Renderer.sharedMaterials,
// MaterialPropertyBlock.HasColor; installed TMP_Text/TMP_Settings source (uGUI 2.6).
if (!UnityEngine.Application.isPlaying || !UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Palette scan requires an existing Editor Play session.");
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating)
    throw new System.InvalidOperationException("Wait for Editor compilation/import to finish before scanning.");

var project = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath, ".."));
var outputDirectory = System.IO.Path.Combine(project, "Logs", "EnvironmentIntegration20261101");
var output = System.IO.Path.Combine(outputDirectory, "visual-palette-scan.json");
if (System.IO.File.Exists(output)) throw new System.IO.IOException("Preserve the existing palette report before rescanning: " + output);
var startedFrame = UnityEngine.Time.frameCount;
var startedTime = UnityEngine.Time.realtimeSinceStartupAsDouble;
var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var materials = new System.Collections.Generic.Dictionary<string, object>();
var renderers = new System.Collections.Generic.List<object>();
var texts = new System.Collections.Generic.List<object>();
var images = new System.Collections.Generic.List<object>();
var particles = new System.Collections.Generic.List<object>();
var errors = new System.Collections.Generic.List<object>();
var defaultMaterials = new System.Collections.Generic.List<object>();
var neutralUntexturedMaterials = new System.Collections.Generic.List<object>();
var neutralNoSpriteImages = new System.Collections.Generic.List<object>();
var neutralBuiltinSpriteImages = new System.Collections.Generic.List<object>();
var whiteAuthoredSpriteImages = new System.Collections.Generic.List<object>();
var whiteAuthoredSpriteRenderers = new System.Collections.Generic.List<object>();
var neutralRuntimeSpriteImages = new System.Collections.Generic.List<object>();
var stockDefaultTexts = new System.Collections.Generic.List<object>();
var styledStockTexts = new System.Collections.Generic.List<object>();
var neutralParticleStarts = new System.Collections.Generic.List<object>();
var configuredFont = TMPro.TMP_Settings.defaultFontAsset;
var configuredSize = TMPro.TMP_Settings.defaultFontSize;

System.Func<UnityEngine.Color, float[]> rgba = c => new[] { c.r, c.g, c.b, c.a };
System.Func<UnityEngine.Color, bool> white = c => UnityEngine.Mathf.Abs(c.r - 1f) <= .001f
    && UnityEngine.Mathf.Abs(c.g - 1f) <= .001f && UnityEngine.Mathf.Abs(c.b - 1f) <= .001f && c.a > .01f;
System.Func<UnityEngine.Color, bool> neutral = c => c.a > .01f && c.r >= .15f && c.r <= 1.05f
    && UnityEngine.Mathf.Max(c.r, c.g, c.b) - UnityEngine.Mathf.Min(c.r, c.g, c.b) <= .015f;
System.Func<UnityEngine.Object, string> assetPath = o => o == null ? "" : UnityEditor.AssetDatabase.GetAssetPath(o);
System.Func<string, bool> authoredPath = p => !string.IsNullOrEmpty(p)
    && (p.StartsWith("Assets/", System.StringComparison.Ordinal) || p.StartsWith("Packages/", System.StringComparison.Ordinal));
System.Func<UnityEngine.Object, object> asset = o => o == null ? null : new {
    entityId = o.GetEntityId().ToString(), name = o.name, type = o.GetType().FullName, path = assetPath(o)
};
System.Func<UnityEngine.Sprite, bool> authoredSprite = s => s != null
    && (authoredPath(assetPath(s)) || authoredPath(assetPath(s.texture)));
System.Func<UnityEngine.Component, string> hierarchy = c => {
    var names = new System.Collections.Generic.List<string>();
    for (var t = c.transform; t != null; t = t.parent) names.Add(t.name + "[" + t.GetSiblingIndex() + "]");
    names.Reverse(); return c.gameObject.scene.name + ":/" + string.Join("/", names);
};
System.Func<UnityEngine.Component, bool> inScope = c => c != null && c.gameObject.activeInHierarchy
    && c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded;
System.Func<UnityEngine.Component, object> identity = c => new {
    entityId = c.GetEntityId().ToString(), path = hierarchy(c), scene = c.gameObject.scene.name,
    activeScene = c.gameObject.scene.handle == activeScene.handle, layer = c.gameObject.layer
};
System.Func<UnityEngine.Material, object> describeMaterial = m => {
    if (m == null) return null;
    if (materials.TryGetValue(m.GetEntityId().ToString(), out var existing)) return existing;
    var colors = new System.Collections.Generic.List<object>();
    var textures = new System.Collections.Generic.List<object>();
    var hasAuthoredTexture = false;
    var hasNeutralBase = false;
    var shader = m.shader;
    if (shader != null) for (var p = 0; p < shader.GetPropertyCount(); p++) {
        var name = shader.GetPropertyName(p);
        if (shader.GetPropertyType(p) == UnityEngine.Rendering.ShaderPropertyType.Color) {
            var color = m.GetColor(name); colors.Add(new { property = name, value = rgba(color) });
            if (name == "_Color" || name == "_BaseColor") hasNeutralBase |= neutral(color);
        }
        if (shader.GetPropertyType(p) == UnityEngine.Rendering.ShaderPropertyType.Texture) {
            var texture = m.GetTexture(name); textures.Add(new { property = name, texture = asset(texture) });
            hasAuthoredTexture |= texture != null && authoredPath(assetPath(texture));
        }
    }
    var materialName = m.name.Replace(" (Instance)", "");
    var defaultNamed = materialName == "Default-Material" || materialName == "Default-Diffuse"
        || materialName == "Default-Line" || materialName == "Default-Particle" || materialName == "Default-Terrain-Standard";
    var result = new { material = asset(m), shader = asset(shader), shaderSupported = shader != null && shader.isSupported,
        colors, textures, renderQueue = m.renderQueue, defaultNameCandidate = defaultNamed,
        neutralNoAuthoredTextureCandidate = hasNeutralBase && !hasAuthoredTexture };
    materials.Add(m.GetEntityId().ToString(), result);
    if (defaultNamed) defaultMaterials.Add(new { material = asset(m), reason = "Exact Unity default material name; confirm use in the frame." });
    if (hasNeutralBase && !hasAuthoredTexture) neutralUntexturedMaterials.Add(new { material = asset(m),
        reason = "Neutral base color without an asset-backed texture; vertex color, procedural art or MPB may be intentional." });
    return result;
};

foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.Renderer>()) {
    if (!inScope(r) || !r.enabled) continue;
    try {
        var slots = new System.Collections.Generic.List<object>();
        var shared = r.sharedMaterials; // Never use material/materials (which instantiate).
        var rendererBlock = new UnityEngine.MaterialPropertyBlock(); r.GetPropertyBlock(rendererBlock);
        for (var slot = 0; slot < shared.Length; slot++) {
            var m = shared[slot]; describeMaterial(m);
            var perMaterialBlock = new UnityEngine.MaterialPropertyBlock(); r.GetPropertyBlock(perMaterialBlock, slot);
            var selectedBlock = perMaterialBlock.isEmpty ? rendererBlock : perMaterialBlock;
            var overrides = new System.Collections.Generic.List<object>();
            if (m != null && m.shader != null) for (var p = 0; p < m.shader.GetPropertyCount(); p++) {
                var name = m.shader.GetPropertyName(p);
                if (m.shader.GetPropertyType(p) == UnityEngine.Rendering.ShaderPropertyType.Color && selectedBlock.HasColor(name))
                    overrides.Add(new { property = name, value = rgba(selectedBlock.GetColor(name)) });
            }
            slots.Add(new { slot, materialId = m == null ? null : m.GetEntityId().ToString(), colorOverrides = overrides,
                overrideScope = perMaterialBlock.isEmpty ? "renderer" : "per-material",
                hasRendererBlock = !rendererBlock.isEmpty, hasPerMaterialBlock = !perMaterialBlock.isEmpty });
        }
        var sr = r as UnityEngine.SpriteRenderer;
        renderers.Add(new { component = identity(r), type = r.GetType().FullName, r.isVisible, r.forceRenderingOff,
            sharedMaterialSlots = slots, sprite = sr == null ? null : asset(sr.sprite),
            spriteTexture = sr == null || sr.sprite == null ? null : asset(sr.sprite.texture),
            spriteColor = sr == null ? null : rgba(sr.color), preservesAuthoredSpriteColor = sr != null && authoredSprite(sr.sprite) && white(sr.color) });
        if (sr != null && authoredSprite(sr.sprite) && white(sr.color)) whiteAuthoredSpriteRenderers.Add(identity(sr));
    } catch (System.Exception ex) { errors.Add(new { scope = "Renderer", component = hierarchy(r), error = ex.ToString() }); }
}

foreach (var t in UnityEngine.Object.FindObjectsByType<TMPro.TMP_Text>()) {
    if (!inScope(t) || !t.isActiveAndEnabled) continue;
    try {
        var font = t.font; var material = t.fontSharedMaterial; describeMaterial(material);
        var stock = font != null && (font.name.StartsWith("LiberationSans", System.StringComparison.OrdinalIgnoreCase)
            || font.name.Equals("Arial SDF", System.StringComparison.OrdinalIgnoreCase));
        var raw = t.text ?? "";
        var colorMarkup = t.richText && (raw.IndexOf("<color", System.StringComparison.OrdinalIgnoreCase) >= 0
            || raw.IndexOf("<#", System.StringComparison.Ordinal) >= 0 || raw.IndexOf("<gradient", System.StringComparison.OrdinalIgnoreCase) >= 0);
        var richMarkup = t.richText && raw.IndexOf('<') >= 0 && raw.IndexOf('>') >= 0;
        var materialStyled = material != null &&
            ((material.HasProperty("_OutlineWidth") && UnityEngine.Mathf.Abs(material.GetFloat("_OutlineWidth")) > .001f)
            || (material.HasProperty("_GlowPower") && material.IsKeywordEnabled("GLOW_ON"))
            || material.IsKeywordEnabled("UNDERLAY_ON") || material.IsKeywordEnabled("UNDERLAY_INNER")
            || (material.HasProperty("_FaceColor") && !white(material.GetColor("_FaceColor"))));
        var defaultMaterial = font != null && material == font.material && !materialStyled;
        var plain = white(t.color) && UnityEngine.Mathf.Abs(t.color.a - 1f) < .001f
            && UnityEngine.Mathf.Abs(t.fontSize - configuredSize) < .001f && t.fontStyle == TMPro.FontStyles.Normal
            && !t.enableAutoSizing && !t.enableVertexGradient && !richMarkup && defaultMaterial
            && UnityEngine.Mathf.Abs(t.characterSpacing) < .001f && UnityEngine.Mathf.Abs(t.wordSpacing) < .001f
            && UnityEngine.Mathf.Abs(t.lineSpacing) < .001f;
        texts.Add(new { component = identity(t), font = asset(font), fontMaterial = asset(material), size = t.fontSize,
            minSize = t.fontSizeMin, maxSize = t.fontSizeMax, autoSize = t.enableAutoSizing, color = rgba(t.color),
            style = t.fontStyle.ToString(), alignment = t.alignment.ToString(), t.raycastTarget, t.richText,
            vertexGradient = t.enableVertexGradient, colorMarkup, richMarkup, materialStyled, textLength = raw.Length,
            stockFont = stock, configuredDefaultFont = font == configuredFont, stockDefaultCandidate = stock && plain });
        if (stock && plain) stockDefaultTexts.Add(identity(t));
        else if (stock) styledStockTexts.Add(new { component = identity(t), reason = "Stock font with explicit size/color/style/material/spacing or gradient markup; not a stock-default candidate." });
    } catch (System.Exception ex) { errors.Add(new { scope = "TMP", component = hierarchy(t), error = ex.ToString() }); }
}

foreach (var i in UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Image>()) {
    if (!inScope(i) || !i.isActiveAndEnabled) continue;
    try {
        var sprite = i.overrideSprite != null ? i.overrideSprite : i.sprite;
        var authored = authoredSprite(sprite);
        var builtin = sprite != null && !authored && (!string.IsNullOrEmpty(assetPath(sprite))
            || !string.IsNullOrEmpty(assetPath(sprite.texture)));
        var effectiveAlpha = i.color.a * i.canvasRenderer.GetInheritedAlpha();
        var eligible = neutral(i.color) && effectiveAlpha > .01f && !i.canvasRenderer.cull
            && (i.canvas == null || i.canvas.isActiveAndEnabled);
        var classification = authored ? (white(i.color) ? "white-tint-preserves-authored-sprite" : "authored-sprite")
            : sprite == null ? "no-sprite" : builtin ? "builtin-sprite" : "runtime-sprite-unclassified";
        images.Add(new { component = identity(i), color = rgba(i.color), effectiveAlpha, i.raycastTarget, i.maskable,
            sprite = asset(sprite), spriteTexture = sprite == null ? null : asset(sprite.texture), classification,
            imageType = i.type.ToString(), i.fillAmount, i.preserveAspect, culled = i.canvasRenderer.cull,
            canvas = i.canvas == null ? null : identity(i.canvas), neutralCandidateEligible = eligible });
        if (eligible && sprite == null) neutralNoSpriteImages.Add(identity(i));
        else if (eligible && builtin) neutralBuiltinSpriteImages.Add(identity(i));
        else if (eligible && !authored) neutralRuntimeSpriteImages.Add(identity(i));
        if (authored && white(i.color)) whiteAuthoredSpriteImages.Add(identity(i));
    } catch (System.Exception ex) { errors.Add(new { scope = "Image", component = hierarchy(i), error = ex.ToString() }); }
}

System.Func<UnityEngine.Gradient, object> describeGradient = g => {
    if (g == null) return null;
    var colorKeys = new System.Collections.Generic.List<object>();
    var alphaKeys = new System.Collections.Generic.List<object>();
    foreach (var k in g.colorKeys) colorKeys.Add(new { k.time, color = rgba(k.color) });
    foreach (var k in g.alphaKeys) alphaKeys.Add(new { k.time, k.alpha });
    return new { mode = g.mode.ToString(), colorKeys, alphaKeys };
};
foreach (var p in UnityEngine.Object.FindObjectsByType<UnityEngine.ParticleSystem>()) {
    if (!inScope(p)) continue;
    try {
        var main = p.main; var c = main.startColor;
        var samples = new System.Collections.Generic.List<float[]>(); var allNeutral = true;
        // Sampling is diagnostic for gradient modes; the full keys below are the authoritative record.
        for (var n = 0; n <= 8; n++) foreach (var factor in new[] { 0f, .5f, 1f }) {
            var sample = c.Evaluate(n / 8f, factor); samples.Add(rgba(sample)); allNeutral &= neutral(sample);
        }
        particles.Add(new { component = identity(p), p.isPlaying, p.isEmitting, p.particleCount, mode = c.mode.ToString(),
            color = rgba(c.color), colorMin = rgba(c.colorMin), colorMax = rgba(c.colorMax),
            gradient = describeGradient(c.gradient), gradientMin = describeGradient(c.gradientMin),
            gradientMax = describeGradient(c.gradientMax), sampledColors = samples,
            colorOverLifetimeEnabled = p.colorOverLifetime.enabled, colorBySpeedEnabled = p.colorBySpeed.enabled,
            neutralStartCandidate = allNeutral });
        if (allNeutral) neutralParticleStarts.Add(new { component = identity(p), p.isPlaying, p.particleCount,
            reason = "Sampled start colors are neutral; texture, later color modules and shader can intentionally supply the palette." });
    } catch (System.Exception ex) { errors.Add(new { scope = "ParticleSystem", component = hierarchy(p), error = ex.ToString() }); }
}

var summary = new {
    counts = new { renderers = renderers.Count, uniqueSharedMaterials = materials.Count, tmpTexts = texts.Count,
        images = images.Count, particleSystems = particles.Count, errors = errors.Count,
        defaultMaterialCandidates = defaultMaterials.Count, neutralNoAuthoredTextureMaterials = neutralUntexturedMaterials.Count,
        neutralNoSpriteImageCandidates = neutralNoSpriteImages.Count, neutralBuiltinSpriteImageCandidates = neutralBuiltinSpriteImages.Count,
        neutralRuntimeSpriteImages = neutralRuntimeSpriteImages.Count, stockDefaultTextCandidates = stockDefaultTexts.Count,
        deliberatelyStyledStockFonts = styledStockTexts.Count, whiteAuthoredSpriteImages = whiteAuthoredSpriteImages.Count,
        whiteAuthoredSpriteRenderers = whiteAuthoredSpriteRenderers.Count, neutralParticleStartCandidates = neutralParticleStarts.Count },
    defaultMaterials, neutralUntexturedMaterials, neutralNoSpriteImages, neutralBuiltinSpriteImages,
    stockDefaultTexts, neutralParticleStarts, whiteAuthoredSpriteImages, whiteAuthoredSpriteRenderers,
    styledStockTexts, neutralRuntimeSpriteImages
};
var report = new {
    schema = 1, capturedUtc = System.DateTime.UtcNow.ToString("O"), project, output,
    engine = UnityEngine.Application.unityVersion, activeScene = activeScene.path,
    scope = "Enabled/active objects in all currently loaded runtime scenes, including persistent objects; asset/prefab objects excluded. Active-scene membership is recorded per object.",
    state = new { startedFrame, finishedFrame = UnityEngine.Time.frameCount, UnityEngine.Application.runInBackground,
        timeScale = UnityEngine.Time.timeScale, colorSpace = UnityEngine.QualitySettings.activeColorSpace.ToString(),
        pipeline = asset(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline),
        elapsedSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble - startedTime },
    thresholds = new { neutralRgbSpread = .015f, neutralMin = .15f, neutralMax = 1.05f, visibleAlphaFloor = .01f, whiteTolerance = .001f },
    configuredTmpDefaults = new { font = asset(configuredFont), size = configuredSize },
    limitations = new[] {
        "Candidate flags are not visual failures or a scorecard pass. Author intent and final captures require separate review.",
        "One instantaneous scene snapshot is not all menus/chapters or animation/VFX coverage. No camera visibility/frustum/occlusion proof.",
        "Renderer.isVisible may include the Scene view. Images record cull/inherited alpha; TMP text can be hidden by masking or CanvasGroup.",
        "Material colors are shared values plus selected color property-block overrides; vertex colors, lighting, shader math, texture pixels and postprocessing are not evaluated.",
        "Authored means asset-backed, not manually approved. Runtime sprites are classified separately, not presumed untextured defaults.",
        "TMP stock detection is limited to LiberationSans/Arial SDF plus plain configured default size/style; rich-text content is not fully rendered/analyzed.",
        "Particle start colors/keys are recorded; gradients are sampled for candidate flags, not a proof of all output colors. Non-ParticleSystem procedural VFX are covered only as renderers.",
        "No scene/material/font/UI/particle changes, time/input/Play state changes, asset refresh/save/import, or player save access. Only this report file is written."
    },
    summary, materials = new System.Collections.Generic.List<object>(materials.Values), renderers, texts, images, particles, errors,
    scanComplete = errors.Count == 0
};
System.IO.Directory.CreateDirectory(outputDirectory);
using (var stream = new System.IO.FileStream(output, System.IO.FileMode.CreateNew, System.IO.FileAccess.Write, System.IO.FileShare.Read))
using (var writer = new System.IO.StreamWriter(stream, new System.Text.UTF8Encoding(false)))
    writer.Write(Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
return new { output, summary.counts }; // Full object lists remain in the JSON; console gets only path/counts.
