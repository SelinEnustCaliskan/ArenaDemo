﻿// Copyright (C) 2014 - 2016 Stephan Bouchard - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms
// Release 0.1.54 Beta 1b.


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;


namespace TMPro
{
    public interface ITextElement
    {
        Material sharedMaterial { get; }

        void Rebuild(CanvasUpdate update);
        int GetInstanceID();
    }

    public enum TextAlignmentOptions
    {
        TopLeft = 0, Top = 1, TopRight = 2, TopJustified = 3,
        Left = 4, Center = 5, Right = 6, Justified = 7,
        BottomLeft = 8, Bottom = 9, BottomRight = 10, BottomJustified = 11,
        BaselineLeft = 12, Baseline = 13, BaselineRight = 14, BaselineJustified = 15,
        MidlineLeft = 16, Midline = 17, MidlineRight = 18, MidlineJustified = 19
    };

    /// <summary>
    /// Flags controlling what vertex data gets pushed to the mesh.
    /// </summary>
    public enum TextRenderFlags {
        DontRender  = 0x0,
        Render      = 0xFF
    };



    public enum TMP_TextElementType { Character, Sprite };
    public enum MaskingTypes { MaskOff = 0, MaskHard = 1, MaskSoft = 2 }; //, MaskTex = 4 };
    public enum TextOverflowModes { Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3, ScrollRect = 4, Page = 5 };
    public enum MaskingOffsetMode { Percentage = 0, Pixel = 1 };
    public enum TextureMappingOptions { Character = 0, Line = 1, Paragraph = 2, MatchAspect = 3 };

    public enum FontStyles { Normal = 0x0, Bold = 0x1, Italic = 0x2, Underline = 0x4, LowerCase = 0x8, UpperCase = 0x10, SmallCaps = 0x20, Strikethrough = 0x40, Superscript = 0x80, Subscript = 0x100 };
    public enum FontWeights { Thin = 100, ExtraLight = 200, Light = 300, Normal = 400, Medium = 500, SemiBold = 600, Bold = 700, Heavy = 800, Black = 900 };

    public enum TagUnits { Pixels = 0, FontUnits = 1, Percentage = 2 };
    public enum TagType { None = 0x0, NumericalValue = 0x1, StringValue = 0x2, ColorValue = 0x4 };


    /// <summary>
    /// Base class which contains common properties and functions shared between the TextMeshPro and TextMeshProUGUI component.
    /// </summary>
    public class TMP_Text : MaskableGraphic
    {
        /// <summary>
        /// A string containing the text to be displayed.
        /// </summary>
        public string text
        {
            get { return m_text; }
            set { if (m_text == value) return; m_text = value; m_inputSource = TextInputSources.Text; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; m_isInputParsingRequired = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected string m_text;


        /// <summary>
        /// The Font Asset to be assigned to this text object.
        /// </summary>
        public TMP_FontAsset font
        {
            get { return m_fontAsset; }
            set { if (m_fontAsset == value) return;  m_fontAsset = value; LoadFontAsset(); m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; m_isInputParsingRequired = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TMP_FontAsset m_fontAsset;
        protected TMP_FontAsset m_currentFontAsset;
        protected bool m_isSDFShader;


        /// <summary>
        /// The material to be assigned to this text object.
        /// </summary>
        public virtual Material fontSharedMaterial
        {
            get { return m_sharedMaterial; }
            set { if (m_sharedMaterial == value) return; SetSharedMaterial(value); m_havePropertiesChanged = true; m_isInputParsingRequired = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material m_sharedMaterial;
        protected Material m_currentMaterial;
        protected MaterialReference[] m_materialReferences = new MaterialReference[32];
        protected Dictionary<int, int> m_materialReferenceIndexLookup = new Dictionary<int, int>();

        protected TMP_XmlTagStack<MaterialReference> m_materialReferenceStack = new TMP_XmlTagStack<MaterialReference>(new MaterialReference[16]);
        protected int m_currentMaterialIndex;
        protected int m_sharedMaterialHashCode;


        /// <summary>
        /// An array containing the materials used by the text object.
        /// </summary>
        public virtual Material[] fontSharedMaterials
        {
            get { return GetSharedMaterials(); }
            set { SetSharedMaterials(value); m_havePropertiesChanged = true; m_isInputParsingRequired = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material[] m_fontSharedMaterials;


        /// <summary>
        /// The material to be assigned to this text object. An instance of the material will be assigned to the object's renderer.
        /// </summary>
        public Material fontMaterial
        {
            // Return an Instance of the current material.
            get { return GetMaterial(m_sharedMaterial); }

            // Assign new font material
            set
            {
                if (m_sharedMaterial != null && m_sharedMaterial.GetInstanceID() == value.GetInstanceID()) return;

                m_sharedMaterial = value;

                m_padding = GetPaddingForMaterial();
                m_havePropertiesChanged = true;
                m_isInputParsingRequired = true;

                SetVerticesDirty();
                SetMaterialDirty();
            }
        }
        [SerializeField]
        protected Material m_fontMaterial;


        /// <summary>
        /// The materials to be assigned to this text object. An instance of the materials will be assigned.
        /// </summary>
        public virtual Material[] fontMaterials
        {
            get { return GetMaterials(m_fontSharedMaterials); }

            set { SetSharedMaterials(value); m_havePropertiesChanged = true; m_isInputParsingRequired = true; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Material[] m_fontMaterials;

        protected bool m_isMaterialDirty;


        /// <summary>
        /// This is the default vertex color assigned to each vertices. Color tags will override vertex colors unless the overrideColorTags is set.
        /// </summary>
        public new Color color
        {
            get { return m_fontColor; }
            set { if (m_fontColor == value) return; m_havePropertiesChanged = true; m_fontColor = value; SetVerticesDirty(); }
        }
        [UnityEngine.Serialization.FormerlySerializedAs("m_fontColor")] // Required for backwards compatibility with pre-Unity 4.6 releases.
        [SerializeField]
        protected Color32 m_fontColor32 = Color.white;
        [SerializeField]
        protected Color m_fontColor = Color.white;
        protected static Color32 s_colorWhite = new Color32(255, 255, 255, 255);


        /// <summary>
        /// Sets the vertex color alpha value.
        /// </summary>
        public float alpha
        {
            get { return m_fontColor.a; }
            set { if (m_fontColor.a == value) return; m_fontColor.a = value; m_havePropertiesChanged = true; SetVerticesDirty(); }
        }


        /// <summary>
        /// Determines if Vertex Color Gradient should be used
        /// </summary>
        /// <value><c>true</c> if enable vertex gradient; otherwise, <c>false</c>.</value>
        public bool enableVertexGradient
        {
            get { return m_enableVertexGradient; }
            set { if (m_enableVertexGradient == value) return; m_havePropertiesChanged = true; m_enableVertexGradient = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_enableVertexGradient;


        /// <summary>
        /// Sets the vertex colors for each of the 4 vertices of the character quads.
        /// </summary>
        /// <value>The color gradient.</value>
        public VertexGradient colorGradient
        {
            get { return m_fontColorGradient; }
            set { m_havePropertiesChanged = true; m_fontColorGradient = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected VertexGradient m_fontColorGradient = new VertexGradient(Color.white);


        /// <summary>
        /// Default Sprite Asset used by the text object.
        /// </summary>
        public TMP_SpriteAsset spriteAsset
        {
            get { return m_spriteAsset; }
            set { m_spriteAsset = value; }
        }
        protected TMP_SpriteAsset m_spriteAsset;


        /// <summary>
        /// Determines whether or not the sprite color is multiplies by the vertex color of the text.
        /// </summary>
        public bool tintAllSprites
        {
            get { return m_tintAllSprites; }
            set { if (m_tintAllSprites == value) return; m_tintAllSprites = value; m_havePropertiesChanged = true; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_tintAllSprites;
        protected bool m_tintSprite;
        protected Color32 m_spriteColor;


        /// <summary>
        /// This overrides the color tags forcing the vertex colors to be the default font color.
        /// </summary>
        public bool overrideColorTags
        {
            get { return m_overrideHtmlColors; }
            set { if (m_overrideHtmlColors == value) return; m_havePropertiesChanged = true; m_overrideHtmlColors = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_overrideHtmlColors = false;


        /// <summary>
        /// Sets the color of the _FaceColor property of the assigned material. Changing face color will result in an instance of the material.
        /// </summary>
        public Color32 faceColor
        {
            get
            {
                if (m_sharedMaterial == null) return m_faceColor;

                m_faceColor = m_sharedMaterial.GetColor(ShaderUtilities.ID_FaceColor);
                return m_faceColor;
            }

            set { if (m_faceColor.Compare(value)) return; SetFaceColor(value); m_havePropertiesChanged = true; m_faceColor = value; SetVerticesDirty(); SetMaterialDirty(); }
        }
        [SerializeField]
        protected Color32 m_faceColor = Color.white;


        /// <summary>
        /// Sets the color of the _OutlineColor property of the assigned material. Changing outline color will result in an instance of the material.
        /// </summary>
        public Color32 outlineColor
        {
            get
            {
                if (m_sharedMaterial == null) return m_outlineColor;

                m_outlineColor = m_sharedMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
                return m_outlineColor;
            }

            set { if (m_outlineColor.Compare(value)) return; SetOutlineColor(value); m_havePropertiesChanged = true; m_outlineColor = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected Color32 m_outlineColor = Color.black;


        /// <summary>
        /// Sets the thickness of the outline of the font. Setting this value will result in an instance of the material.
        /// </summary>
        public float outlineWidth
        {
            get
            {
                if (m_sharedMaterial == null) return m_outlineWidth;

                m_outlineWidth = m_sharedMaterial.GetFloat(ShaderUtilities.ID_OutlineWidth);
                return m_outlineWidth;
            }
            set { if (m_outlineWidth == value) return; SetOutlineThickness(value); m_havePropertiesChanged = true; m_outlineWidth = value; SetVerticesDirty(); }
        }
        protected float m_outlineWidth = 0.0f;


        /// <summary>
        /// The point size of the font.
        /// </summary>
        public float fontSize
        {
            get { return m_fontSize; }
            set { if (m_fontSize == value) return; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_fontSize = value; if (!m_enableAutoSizing) m_fontSizeBase = m_fontSize; }
        }
        [SerializeField]
        protected float m_fontSize = 36; // Font Size
        protected float m_currentFontSize; // Temporary Font Size affected by tags
        [SerializeField]
        protected float m_fontSizeBase = 36;
        protected TMP_XmlTagStack<float> m_sizeStack = new TMP_XmlTagStack<float>(new float[16]);


        /// <summary>
        /// The scale of the current text.
        /// </summary>
        public float fontScale
        {
            get { return m_fontScale; }
        }


        /// <summary>
        /// Control the weight of the font if an alternative font asset is assigned for the given weight in the font asset editor.
        /// </summary>
        public int fontWeight
        {
            get { return m_fontWeight; }
            set { if (m_fontWeight == value) return; m_fontWeight = value;  m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected int m_fontWeight = 400;
        protected int m_fontWeightInternal;
        protected TMP_XmlTagStack<int> m_fontWeightStack = new TMP_XmlTagStack<int>(new int[16]);

        /// <summary>
        /// 
        /// </summary>
        public float pixelsPerUnit
        {
            get
            {
                var localCanvas = canvas;
                if (!localCanvas)
                    return 1;
                // For dynamic fonts, ensure we use one pixel per pixel on the screen.
                if (!font)
                    return localCanvas.scaleFactor;
                // For non-dynamic fonts, calculate pixels per unit based on specified font size relative to font object's own font size.
                if (m_currentFontAsset == null || m_currentFontAsset.fontInfo.PointSize <= 0 ||  m_fontSize <= 0)
                    return 1;
                return m_fontSize / m_currentFontAsset.fontInfo.PointSize;
            }
        }


        /// <summary>
        /// Enable text auto-sizing
        /// </summary>
        public bool enableAutoSizing
        {
            get { return m_enableAutoSizing; }
            set { if (m_enableAutoSizing == value) return; m_enableAutoSizing = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_enableAutoSizing;
        protected float m_maxFontSize; // Used in conjunction with auto-sizing
        protected float m_minFontSize; // Used in conjunction with auto-sizing


        /// <summary>
        /// Minimum point size of the font when text auto-sizing is enabled.
        /// </summary>
        public float fontSizeMin
        {
            get { return m_fontSizeMin; }
            set { if (m_fontSizeMin == value) return; m_fontSizeMin = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSizeMin = 0; // Text Auto Sizing Min Font Size.

        /// <summary>
        /// Maximum point size of the font when text auto-sizing is enabled.
        /// </summary>
        public float fontSizeMax
        {
            get { return m_fontSizeMax; }
            set { if (m_fontSizeMax == value) return; m_fontSizeMax = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_fontSizeMax = 0; // Text Auto Sizing Max Font Size.


        /// <summary>
        /// The style of the text
        /// </summary>
        public FontStyles fontStyle
        {
            get { return m_fontStyle; }
            set { if (m_fontStyle == value) return; m_fontStyle = value; m_havePropertiesChanged = true; checkPaddingRequired = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected FontStyles m_fontStyle = FontStyles.Normal;
        protected FontStyles m_style = FontStyles.Normal;


        /// <summary>
        /// Property used in conjunction with padding calculation for the geometry.
        /// </summary>
        public bool isUsingBold { get { return m_isUsingBold; } }
        protected bool m_isUsingBold = false; // Used to ensure GetPadding & Ratios take into consideration bold characters.


        /// <summary>
        /// Text alignment options
        /// </summary>
        public TextAlignmentOptions alignment
        {
            get { return m_textAlignment; }
            set { if (m_textAlignment == value) return;  m_havePropertiesChanged = true; m_textAlignment = value; SetVerticesDirty(); }
        }
        [SerializeField]
        [UnityEngine.Serialization.FormerlySerializedAs("m_lineJustification")]
        protected TextAlignmentOptions m_textAlignment = TextAlignmentOptions.TopLeft;
        protected TextAlignmentOptions m_lineJustification;
        protected Vector3[] m_textContainerLocalCorners = new Vector3[4];


        /// <summary>
        /// The amount of additional spacing between characters.
        /// </summary>
        public float characterSpacing
        {
            get { return m_characterSpacing; }
            set { if (m_characterSpacing == value) return; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_characterSpacing = value; }
        }
        [SerializeField]
        protected float m_characterSpacing = 0;
        protected float m_cSpacing = 0;
        protected float m_monoSpacing = 0;


        /// <summary>
        /// The amount of additional spacing to add between each lines of text.
        /// </summary>
        public float lineSpacing
        {
            get { return m_lineSpacing; }
            set { if (m_lineSpacing == value) return; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_lineSpacing = value; }
        }
        [SerializeField]
        protected float m_lineSpacing = 0;
        protected float m_lineSpacingDelta = 0; // Used with Text Auto Sizing feature
        protected float m_lineHeight = 0; // Used with the <line-height=xx.x> tag.
        [SerializeField]
        protected float m_lineSpacingMax = 0; // Text Auto Sizing Max Line spacing reduction.

        /// <summary>
        /// The amount of additional spacing to add between each lines of text.
        /// </summary>
        public float paragraphSpacing
        {
            get { return m_paragraphSpacing; }
            set { if (m_paragraphSpacing == value) return; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_paragraphSpacing = value; }
        }
        [SerializeField]
        protected float m_paragraphSpacing = 0;


        /// <summary>
        /// Percentage the width of characters can be adjusted before text auto-sizing begins to reduce the point size.
        /// </summary>
        public float characterWidthAdjustment
        {
            get { return m_charWidthMaxAdj; }
            set { if (m_charWidthMaxAdj == value) return; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_charWidthMaxAdj = value; }
        }
        [SerializeField]
        protected float m_charWidthMaxAdj = 0f; // Text Auto Sizing Max Character Width reduction.
        protected float m_charWidthAdjDelta = 0;


        /// <summary>
        /// Controls whether or not word wrapping is applied. When disabled, the text will be displayed on a single line.
        /// </summary>
        public bool enableWordWrapping
        {
            get { return m_enableWordWrapping; }
            set { if (m_enableWordWrapping == value) return; m_havePropertiesChanged = true; m_isInputParsingRequired = true; m_isCalculateSizeRequired = true; m_enableWordWrapping = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_enableWordWrapping = false;
        protected bool m_isCharacterWrappingEnabled = false;
        protected bool m_isNonBreakingSpace = false;
        protected bool m_isIgnoringAlignment;


        public float wordWrappingRatios
        {
            get { return m_wordWrappingRatios; }
            set { if (m_wordWrappingRatios == value) return; m_wordWrappingRatios = value; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected float m_wordWrappingRatios = 0.4f; // Controls word wrapping ratios between word or characters.

        /// <summary>
        /// Controls the Text Overflow Mode
        /// </summary>
        public TextOverflowModes OverflowMode
        {
            get { return m_overflowMode; }
            set { if (m_overflowMode == value) return; m_overflowMode = value; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected TextOverflowModes m_overflowMode = TextOverflowModes.Overflow;
        protected bool m_isTextTruncated;

        /// <summary>
        /// Determines if kerning is enabled or disabled.
        /// </summary>
        public bool enableKerning
        {
            get { return m_enableKerning; }
            set { if (m_enableKerning == value) return; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_enableKerning = value; }
        }
        [SerializeField]
        protected bool m_enableKerning;


        /// <summary>
        /// Adds extra padding around each character. This may be necessary when the displayed text is very small to prevent clipping.
        /// </summary>
        public bool extraPadding
        {
            get { return m_enableExtraPadding; }
            set { if (m_enableExtraPadding == value) return; m_havePropertiesChanged = true; m_enableExtraPadding = value; UpdateMeshPadding(); /* m_isCalculateSizeRequired = true;*/ SetVerticesDirty(); /* SetLayoutDirty();*/ }
        }
        [SerializeField]
        protected bool m_enableExtraPadding = false;
        [SerializeField]
        protected bool checkPaddingRequired;


        /// <summary>
        /// Enables or Disables Rich Text Tags
        /// </summary>
        public bool richText
        {
            get { return m_isRichText; }
            set { if (m_isRichText == value) return; m_isRichText = value; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_isInputParsingRequired = true; }
        }
        [SerializeField]
        protected bool m_isRichText = true; // Used to enable or disable Rich Text.


        /// <summary>
        /// Enables or Disables parsing of CTRL characters in input text.
        /// </summary>
        public bool parseCtrlCharacters
        {
            get { return m_parseCtrlCharacters; }
            set { if (m_parseCtrlCharacters == value) return; m_parseCtrlCharacters = value; m_havePropertiesChanged = true; m_isCalculateSizeRequired = true; SetVerticesDirty(); SetLayoutDirty(); m_isInputParsingRequired = true; }
        }
        protected bool m_parseCtrlCharacters = true;


        /// <summary>
        /// Sets the RenderQueue along with Ztest to force the text to be drawn last and on top of scene elements.
        /// </summary>
        public bool isOverlay
        {
            get { return m_isOverlay; }
            set { if (m_isOverlay == value) return; m_isOverlay = value; SetShaderDepth(); m_havePropertiesChanged = true; SetVerticesDirty(); }
        }
        protected bool m_isOverlay = false;


        /// <summary>
        /// Sets Perspective Correction to Zero for Orthographic Camera mode & 0.875f for Perspective Camera mode.
        /// </summary>
        public bool isOrthographic
        {
            get { return m_isOrthographic; }
            set { if (m_isOrthographic == value) return; m_havePropertiesChanged = true; m_isOrthographic = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected bool m_isOrthographic = false;


        /// <summary>
        /// Sets the culling on the shaders. Note changing this value will result in an instance of the material.
        /// </summary>
        public bool enableCulling
        {
            get { return m_isCullingEnabled; }
            set { if (m_isCullingEnabled == value) return; m_isCullingEnabled = value; SetCulling(); m_havePropertiesChanged = true; }
        }
        [SerializeField]
        protected bool m_isCullingEnabled = false;


        /// <summary>
        /// Forces objects that are not visible to get refreshed.
        /// </summary>
        public bool ignoreVisibility
        {
            get { return m_ignoreCulling; }
            set { if (m_ignoreCulling == value) return; m_havePropertiesChanged = true; m_ignoreCulling = value; }
        }
        [SerializeField]
        protected bool m_ignoreCulling = true; // Not implemented yet.


        /// <summary>
        /// Controls how the face and outline textures will be applied to the text object.
        /// </summary>
        public TextureMappingOptions horizontalMapping
        {
            get { return m_horizontalMapping; }
            set { if (m_horizontalMapping == value) return; m_havePropertiesChanged = true; m_horizontalMapping = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected TextureMappingOptions m_horizontalMapping = TextureMappingOptions.Character;


        /// <summary>
        /// Controls how the face and outline textures will be applied to the text object.
        /// </summary>
        public TextureMappingOptions verticalMapping
        {
            get { return m_verticalMapping; }
            set { if (m_verticalMapping == value) return;  m_havePropertiesChanged = true; m_verticalMapping = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected TextureMappingOptions m_verticalMapping = TextureMappingOptions.Character;


        /// <summary>
        /// Determines if the Mesh will be rendered.
        /// </summary>
        public TextRenderFlags renderMode
        {
            get { return m_renderMode; }
            set { if (m_renderMode == value) return; m_renderMode = value; m_havePropertiesChanged = true; }
        }
        protected TextRenderFlags m_renderMode = TextRenderFlags.Render;


        /// <summary>
        /// Allows to control how many characters are visible from the input.
        /// </summary>
        public int maxVisibleCharacters
        {
            get { return m_maxVisibleCharacters; }
            set { if (m_maxVisibleCharacters == value) return; m_havePropertiesChanged = true; m_maxVisibleCharacters = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleCharacters = 99999;


        /// <summary>
        /// Allows to control how many words are visible from the input.
        /// </summary>
        public int maxVisibleWords
        {
            get { return m_maxVisibleWords; }
            set { if (m_maxVisibleWords == value) return; m_havePropertiesChanged = true; m_maxVisibleWords = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleWords = 99999;


        /// <summary>
        /// Allows control over how many lines of text are displayed.
        /// </summary>
        public int maxVisibleLines
        {
            get { return m_maxVisibleLines; }
            set { if (m_maxVisibleLines == value) return;  m_havePropertiesChanged = true; m_isInputParsingRequired = true; m_maxVisibleLines = value; SetVerticesDirty(); }
        }
        protected int m_maxVisibleLines = 99999;

        /// <summary>
        /// Controls which page of text is shown
        /// </summary>
        public int pageToDisplay
        {
            get { return m_pageToDisplay; }
            set { if (m_pageToDisplay == value) return; m_havePropertiesChanged = true; m_pageToDisplay = value; SetVerticesDirty(); }
        }
        [SerializeField]
        protected int m_pageToDisplay = 1;
        protected bool m_isNewPage = false;

        /// <summary>
        /// The margins of the text object.
        /// </summary>
        public virtual Vector4 margin
        {
            get { return m_margin; }
            set { if (m_margin == value) return; m_margin = value; ComputeMarginSize(); m_havePropertiesChanged = true; SetVerticesDirty(); }
        }
        [SerializeField]
        protected Vector4 m_margin = new Vector4(0, 0, 0, 0);
        protected float m_marginLeft;
        protected float m_marginRight;
        protected float m_marginWidth;
        protected float m_marginHeight;
        protected float m_width = -1;


        /// <summary>
        /// Returns data about the text object which includes information about each character, word, line, link, etc.
        /// </summary>
        public TMP_TextInfo textInfo
        {
            get { return m_textInfo; }
        }
        [SerializeField]
        protected TMP_TextInfo m_textInfo; // Class which holds information about the Text object such as characters, lines, mesh data as well as metrics. 

        /// <summary>
        /// Property tracking if any of the text properties have changed. Flag is set before the text is regenerated.
        /// </summary>
        public bool havePropertiesChanged
        {
            get { return m_havePropertiesChanged; }
            set { if (m_havePropertiesChanged == value) return; m_havePropertiesChanged = value; SetVerticesDirty(); SetLayoutDirty(); }
        }
        [SerializeField]
        protected bool m_havePropertiesChanged;  // Used to track when properties of the text object have changed.


        /// <summary>
        /// Property to handle legacy animation component.
        /// </summary>
        public bool isUsingLegacyAnimationComponent
        {
            get { return m_isUsingLegacyAnimationComponent; }
            set { m_isUsingLegacyAnimationComponent = value; }
        }
        [SerializeField]
        protected bool m_isUsingLegacyAnimationComponent;


        /// <summary>
        /// Returns are reference to the Transform
        /// </summary>
        public new Transform transform
        {
            get
            {
                if (m_transform == null)
                    m_transform = GetComponent<Transform>();
                return m_transform;
            }
        }
        protected Transform m_transform;


        /// <summary>
        /// Returns are reference to the RectTransform
        /// </summary>
        public new RectTransform rectTransform
        {
            get
            {
                if (m_rectTransform == null)
                    m_rectTransform = GetComponent<RectTransform>();
                return m_rectTransform;
            }
        }
        protected RectTransform m_rectTransform;


        /// <summary>
        /// Enables control over setting the size of the text container to match the text object.
        /// </summary>
        public virtual bool autoSizeTextContainer
        {
            get;
            set;
        }


        /// <summary>
        /// The mesh used by the font asset and material assigned to the text object.
        /// </summary>
        public virtual Mesh mesh
        {
            get { return m_mesh; }
        }
        protected Mesh m_mesh;


        /// <summary>
        /// Contains the bounds of the text object.
        /// </summary>
        public virtual Bounds bounds { get; set; }


        // *** PROPERTIES RELATED TO UNITY LAYOUT SYSTEM ***
        /// <summary>
        /// 
        /// </summary>
        public float flexibleHeight { get { return m_flexibleHeight; } }
        protected float m_flexibleHeight = -1f;

        /// <summary>
        /// 
        /// </summary>
        public float flexibleWidth { get { return m_flexibleWidth; } }
        protected float m_flexibleWidth = -1f;

        /// <summary>
        /// 
        /// </summary>
        public float minHeight { get { return m_minHeight; } }
        protected float m_minHeight;

        /// <summary>
        /// 
        /// </summary>
        public float minWidth { get { return m_minWidth; } }
        protected float m_minWidth;


        /// <summary>
        /// Computed preferred width of the text object.
        /// </summary>
        //public virtual float preferredWidth { get { return m_preferredWidth == 9999 ? m_renderedWidth : m_preferredWidth; } }
        public virtual float preferredWidth { get { return m_preferredWidth == 9999 ? GetPreferredWidth() : m_preferredWidth; } }
        protected float m_preferredWidth = 9999;
        protected float m_renderedWidth;


        /// <summary>
        /// Computed preferred height of the text object.
        /// </summary>
        //public virtual float preferredHeight { get { return m_preferredHeight == 9999 ? m_renderedHeight : m_preferredHeight; } }
        public virtual float preferredHeight { get { return m_preferredHeight == 9999 ? GetPreferredHeight() : m_preferredHeight; } }
        protected float m_preferredHeight = 9999;
        protected float m_renderedHeight;

        /// <summary>
        /// 
        /// </summary>
        public int layoutPriority { get { return m_layoutPriority; } }
        protected int m_layoutPriority = 0;

        protected bool m_isCalculateSizeRequired = false;
        protected bool m_isLayoutDirty;

        protected bool m_verticesAlreadyDirty;
        protected bool m_layoutAlreadyDirty;


        [SerializeField]
        protected bool m_isInputParsingRequired = false; // Used to determine if the input text needs to be re-parsed.

        [SerializeField]
        protected bool m_isRightToLeft = false;

        // Protected Fields
        protected enum TextInputSources { Text = 0, SetText = 1, SetCharArray = 2 };
        [SerializeField]
        protected TextInputSources m_inputSource;
        protected string old_text; // Used by SetText to determine if the text has changed.
        protected float old_arg0, old_arg1, old_arg2; // Used by SetText to determine if the args have changed.


        protected float m_fontScale; // Scaling of the font based on Atlas true Font Size and Rendered Font Size.  
        protected float m_fontScaleMultiplier; // Used for handling of superscript and subscript.

        protected char[] m_htmlTag = new char[64]; // Maximum length of rich text tag. This is preallocated to avoid GC.
        protected XML_TagAttribute[] m_xmlAttribute = new XML_TagAttribute[8];

        protected float tag_LineIndent = 0;
        protected float tag_Indent = 0;
        protected TMP_XmlTagStack<float> m_indentStack = new TMP_XmlTagStack<float>(new float[16]);
        protected bool tag_NoParsing;
        //protected TMP_LinkInfo tag_LinkInfo = new TMP_LinkInfo();

        protected bool m_isParsingText;


        protected int[] m_char_buffer; // This array holds the characters to be processed by GenerateMesh();
        private TMP_CharacterInfo[] m_internalCharacterInfo; // Used by functions to calculate preferred values.
        protected char[] m_input_CharArray = new char[256]; // This array hold the characters from the SetText();
        private int m_charArray_Length = 0;
        protected int m_totalCharacterCount;


        // Fields whose state is saved in conjunction with text parsing and word wrapping.
        protected int m_characterCount;
        protected int m_visibleCharacterCount;
        protected int m_visibleSpriteCount;
        protected int m_firstCharacterOfLine;
        protected int m_firstVisibleCharacterOfLine;
        protected int m_lastCharacterOfLine;
        protected int m_lastVisibleCharacterOfLine;
        protected int m_lineNumber;
        protected int m_pageNumber;
        protected float m_maxAscender;
        protected float m_maxDescender;
        protected float m_maxLineAscender;
        protected float m_maxLineDescender;
        protected float m_startOfLineAscender;
        //protected float m_maxFontScale;
        protected float m_lineOffset;
        protected Extents m_meshExtents;



        // Fields used for vertex colors
        protected Color32 m_htmlColor = new Color(255, 255, 255, 128);
        protected TMP_XmlTagStack<Color32> m_colorStack = new TMP_XmlTagStack<Color32>(new Color32[16]);

        protected float m_tabSpacing = 0;
        protected float m_spacing = 0;


        protected bool IsRectTransformDriven;


        // STYLE TAGS
        protected TMP_XmlTagStack<int> m_styleStack = new TMP_XmlTagStack<int>(new int[16]);
        protected TMP_XmlTagStack<int> m_actionStack = new TMP_XmlTagStack<int>(new int[16]);

        protected float m_padding = 0;
        protected float m_baselineOffset; // Used for superscript and subscript.
        protected float m_xAdvance; // Tracks x advancement from character to character.

        protected TMP_TextElementType m_textElementType;
        protected TMP_TextElement m_cached_TextElement; // Glyph / Character information is cached into this variable which is faster than having to fetch from the Dictionary multiple times.
        protected TMP_Glyph m_cached_Underline_GlyphInfo; // Same as above but for the underline character which is used for Underline.
        protected TMP_Glyph m_cached_Ellipsis_GlyphInfo;

        protected TMP_SpriteAsset m_defaultSpriteAsset;
        protected TMP_SpriteAsset m_currentSpriteAsset;
        protected int m_spriteCount = 0;
        protected int m_spriteIndex;
        protected InlineGraphicManager m_inlineGraphics;


        /// <summary>
        /// Method which derived classes need to override to load Font Assets.
        /// </summary>
        protected virtual void LoadFontAsset() { }

        /// <summary>
        /// Function called internally when a new shared material is assigned via the fontSharedMaterial property.
        /// </summary>
        /// <param name="mat"></param>
        protected virtual void SetSharedMaterial(Material mat) { }

        /// <summary>
        /// Function called internally when a new material is assigned via the fontMaterial property.
        /// </summary>
        protected virtual Material GetMaterial(Material mat) { return null; }

        /// <summary>
        /// Function called internally when assigning a new base material.
        /// </summary>
        /// <param name="mat"></param>
        protected virtual void SetFontBaseMaterial(Material mat) { }

        /// <summary>
        /// Method which returns an array containing the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Material[] GetSharedMaterials() { return null; }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void SetSharedMaterials(Material[] materials) { }

        /// <summary>
        /// Method returning instances of the materials used by the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Material[] GetMaterials(Material[] mats) { return null; }

        /// <summary>
        /// Method to set the materials of the text and sub text objects.
        /// </summary>
        /// <param name="mats"></param>
        //protected virtual void SetMaterials (Material[] mats) { }

        /// <summary>
        /// Function used to create an instance of the material
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        protected virtual Material CreateMaterialInstance(Material source)
        {
            Material mat = new Material(source);
            mat.shaderKeywords = source.shaderKeywords;
            mat.name += " (Instance)";

            return mat;
        }

        /// <summary>
        /// Function called internally to set the face color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected virtual void SetFaceColor(Color32 color) { }

        /// <summary>
        /// Function called internally to set the outline color of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="color"></param>
        protected virtual void SetOutlineColor(Color32 color) { }

        /// <summary>
        /// Function called internally to set the outline thickness property of the material. This will results in an instance of the material.
        /// </summary>
        /// <param name="thickness"></param>
        protected virtual void SetOutlineThickness(float thickness) { }

        /// <summary>
        /// Set the Render Queue and ZTest mode on the current material
        /// </summary>
        protected virtual void SetShaderDepth() { }

        /// <summary>
        /// Set the culling mode on the material.
        /// </summary>
        protected virtual void SetCulling() { }

        /// <summary>
        /// Get the padding value for the currently assigned material
        /// </summary>
        /// <returns></returns>
        protected virtual float GetPaddingForMaterial() { return 0; }


        /// <summary>
        /// Get the padding value for the given material
        /// </summary>
        /// <returns></returns>
        protected virtual float GetPaddingForMaterial(Material mat) { return 0; }


        /// <summary>
        /// Method to return the local corners of the Text Container or RectTransform.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector3[] GetTextContainerLocalCorners() { return null; }


        // PUBLIC FUNCTIONS

        /// <summary>
        /// Function to force the regeneration of the text object.
        /// </summary>
        public virtual void ForceMeshUpdate() { }


        /// <summary>
        /// Function to force the regeneration of the text object.
        /// </summary>
        /// <param name="flags"> Flags to control which portions of the geometry gets uploaded.</param>
        //public virtual void ForceMeshUpdate(TMP_VertexDataUpdateFlags flags) { }


        /// <summary>
        /// Function to update the geometry of the main and sub text objects.
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="index"></param>
        public virtual void UpdateGeometry(Mesh mesh, int index) { }


        /// <summary>
        /// Function to push the updated vertex data into the mesh and renderer.
        /// </summary>
        public virtual void UpdateVertexData(TMP_VertexDataUpdateFlags flags) { }


        /// <summary>
        /// Function to push the updated vertex data into the mesh and renderer.
        /// </summary>
        public virtual void UpdateVertexData() { }


        /// <summary>
        /// Function to push a new set of vertices to the mesh.
        /// </summary>
        /// <param name="vertices"></param>
        public virtual void SetVertices(Vector3[] vertices) { }


        /// <summary>
        /// Function to be used to force recomputing of character padding when Shader / Material properties have been changed via script.
        /// </summary>
        public virtual void UpdateMeshPadding() { }


        /// <summary>
        /// 
        /// </summary>
        //public virtual new void UpdateGeometry() { }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="text"></param>
        public void SetText(string text)
        {
            StringToCharArray(text, ref m_char_buffer);

            m_inputSource = TextInputSources.SetCharArray;
            m_isInputParsingRequired = true;
            m_havePropertiesChanged = true;
            m_isCalculateSizeRequired = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>ex. TextMeshPro.SetText ("Number is {0:1}.", 5.56f);</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">String containing the pattern."</param>
        /// <param name="arg0">Value is a float.</param>
        public void SetText(string text, float arg0)
        {
            SetText(text, arg0, 255, 255);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>ex. TextMeshPro.SetText ("First number is {0} and second is {1:2}.", 10, 5.756f);</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">String containing the pattern."</param>
        /// <param name="arg0">Value is a float.</param>
        /// <param name="arg1">Value is a float.</param>
        public void SetText(string text, float arg0, float arg1)
        {
            SetText(text, arg0, arg1, 255);
        }

        /// <summary>
        /// <para>Formatted string containing a pattern and a value representing the text to be rendered.</para>
        /// <para>ex. TextMeshPro.SetText ("A = {0}, B = {1} and C = {2}.", 2, 5, 7);</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">String containing the pattern."</param>
        /// <param name="arg0">Value is a float.</param>
        /// <param name="arg1">Value is a float.</param>
        /// <param name="arg2">Value is a float.</param>
        public void SetText(string text, float arg0, float arg1, float arg2)
        {
            // Early out if nothing has been changed from previous invocation.
            if (text == old_text && arg0 == old_arg0 && arg1 == old_arg1 && arg2 == old_arg2)
            {
                return;
            }

            old_text = text;
            old_arg1 = 255;
            old_arg2 = 255;

            int decimalPrecision = 0;
            int index = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c == 123) // '{'
                {
                    // Check if user is requesting some decimal precision. Format is {0:2}
                    if (text[i + 2] == 58) // ':'
                    {
                        decimalPrecision = text[i + 3] - 48;
                    }

                    switch (text[i + 1] - 48)
                    {
                        case 0: // 1st Arg
                            old_arg0 = arg0;
                            AddFloatToCharArray(arg0, ref index, decimalPrecision);
                            break;
                        case 1: // 2nd Arg
                            old_arg1 = arg1;
                            AddFloatToCharArray(arg1, ref index, decimalPrecision);
                            break;
                        case 2: // 3rd Arg
                            old_arg2 = arg2;
                            AddFloatToCharArray(arg2, ref index, decimalPrecision);
                            break;
                    }

                    if (text[i + 2] == 58)
                        i += 4;
                    else
                        i += 2;

                    continue;
                }
                m_input_CharArray[index] = c;
                index += 1;
            }

            m_input_CharArray[index] = (char)0;
            m_charArray_Length = index; // Set the length to where this '0' termination is.

#if UNITY_EDITOR
            // Create new string to be displayed in the Input Text Box of the Editor Panel.
            m_text = new string(m_input_CharArray, 0, index);
#endif

            m_inputSource = TextInputSources.SetText;
            m_isInputParsingRequired = true;
            m_havePropertiesChanged = true;
            m_isCalculateSizeRequired = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Set the text using a StringBuilder.
        /// </summary>
        /// <description>
        /// Using a StringBuilder instead of concatenating strings prevents memory pollution with temporary objects.
        /// </description>
        /// <param name="text">StringBuilder with text to display.</param>
        public void SetText(StringBuilder text)
        {
            StringBuilderToIntArray(text, ref m_char_buffer);

            m_inputSource = TextInputSources.SetCharArray;
            m_isInputParsingRequired = true;
            m_havePropertiesChanged = true;
            m_isCalculateSizeRequired = true;

            SetVerticesDirty();
            SetLayoutDirty();
        }


        /// <summary>
        /// Character array containing the text to be displayed.
        /// </summary>
        /// <param name="charArray"></param>
        public void SetCharArray(char[] charArray)
        {
            if (charArray == null || charArray.Length == 0)
                return;

            // Check to make sure chars_buffer is large enough to hold the content of the string.
            if (m_char_buffer.Length <= charArray.Length)
            {
                int newSize = Mathf.NextPowerOfTwo(charArray.Length + 1);
                m_char_buffer = new int[newSize];
            }

            int index = 0;

            for (int i = 0; i < charArray.Length; i++)
            {
                if (charArray[i] == 92 && i < charArray.Length - 1)
                {
                    switch ((int)charArray[i + 1])
                    {
                        case 110: // \n LineFeed
                            m_char_buffer[index] = (char)10;
                            i += 1;
                            index += 1;
                            continue;
                        case 114: // \r LineFeed
                            m_char_buffer[index] = (char)13;
                            i += 1;
                            index += 1;
                            continue;
                        case 116: // \t Tab
                            m_char_buffer[index] = (char)9;
                            i += 1;
                            index += 1;
                            continue;
                    }
                }

                m_char_buffer[index] = charArray[i];
                index += 1;
            }
            m_char_buffer[index] = (char)0;

            m_inputSource = TextInputSources.SetCharArray;
            m_havePropertiesChanged = true;
            m_isInputParsingRequired = true;
        }


        /// <summary>
        /// Copies Content of formatted SetText() to charBuffer.
        /// </summary>
        /// <param name="charArray"></param>
        /// <param name="charBuffer"></param>
        protected void SetTextArrayToCharArray(char[] charArray, ref int[] charBuffer)
        {
            //Debug.Log("SetText Array to Char called.");
            if (charArray == null || m_charArray_Length == 0)
                return;

            // Check to make sure chars_buffer is large enough to hold the content of the string.
            if (charBuffer.Length <= m_charArray_Length)
            {
                int newSize = m_charArray_Length > 1024 ? m_charArray_Length + 256 : Mathf.NextPowerOfTwo(m_charArray_Length + 1);
                charBuffer = new int[newSize];
            }

            int index = 0;

            for (int i = 0; i < m_charArray_Length; i++)
            {
                // Handle UTF-32 in the input text (string).
                if (char.IsHighSurrogate(charArray[i]) && char.IsLowSurrogate(charArray[i + 1]))
                {
                    charBuffer[index] = char.ConvertToUtf32(charArray[i], charArray[i + 1]);
                    i += 1;
                    index += 1;
                    continue;
                }

                charBuffer[index] = charArray[i];
                index += 1;
            }
            charBuffer[index] = 0;
        }


        /// <summary>
        /// Method to store the content of a string into an integer array.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="chars"></param>
        protected void StringToCharArray(string text, ref int[] chars)
        {
            if (text == null)
            {
                chars[0] = 0;
                return;
            }

            // Check to make sure chars_buffer is large enough to hold the content of the string.
            if (chars == null || chars.Length <= text.Length)
            {
                int newSize = text.Length > 1024 ? text.Length + 256 : Mathf.NextPowerOfTwo(text.Length + 1);
                chars = new int[newSize];
            }

            int index = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (m_parseCtrlCharacters && text[i] == 92 && text.Length > i + 1)
                {
                    switch ((int)text[i + 1])
                    {
                        case 85: // \U00000000 for UTF-32 Unicode
                            if (text.Length > i + 9)
                            {
                                chars[index] = GetUTF32(i + 2);
                                i += 9;
                                index += 1;
                                continue;
                            }
                            break;
                        case 92: // \ escape
                            if (text.Length <= i + 2) break;
                            chars[index] = text[i + 1];
                            chars[index + 1] = text[i + 2];
                            i += 2;
                            index += 2;
                            continue;
                        case 110: // \n LineFeed
                            chars[index] = (char)10;
                            i += 1;
                            index += 1;
                            continue;
                        case 114: // \r
                            chars[index] = (char)13;
                            i += 1;
                            index += 1;
                            continue;
                        case 116: // \t Tab
                            chars[index] = (char)9;
                            i += 1;
                            index += 1;
                            continue;
                        case 117: // \u0000 for UTF-16 Unicode
                            if (text.Length > i + 5)
                            {
                                chars[index] = (char)GetUTF16(i + 2);
                                i += 5;
                                index += 1;
                                continue;
                            }
                            break;
                    }
                }

                // Handle UTF-32 in the input text (string).
                if (char.IsHighSurrogate(text[i]) && char.IsLowSurrogate(text[i + 1]))
                {
                    chars[index] = char.ConvertToUtf32(text[i], text[i + 1]);
                    i += 1;
                    index += 1;
                    continue;
                }

                chars[index] = text[i];
                index += 1;
            }
            chars[index] = (char)0;
        }


        /// <summary>
        /// Copy contents of StringBuilder into int array.
        /// </summary>
        /// <param name="text">Text to copy.</param>
        /// <param name="chars">Array to store contents.</param>
        protected void StringBuilderToIntArray(StringBuilder text, ref int[] chars)
        {
            if (text == null)
            {
                chars[0] = 0;
                return;
            }

            // Check to make sure chars_buffer is large enough to hold the content of the string.
            if (chars == null || chars.Length <= text.Length)
            {
                int newSize = text.Length > 1024 ? text.Length + 256 : Mathf.NextPowerOfTwo(text.Length + 1);
                //Debug.Log("Resizing the chars_buffer[" + chars.Length + "] to chars_buffer[" + newSize + "].");
                chars = new int[newSize];
            }

            int index = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (m_parseCtrlCharacters && text[i] == 92 && text.Length > i + 1)
                {
                    switch ((int)text[i + 1])
                    {
                        case 85: // \U00000000 for UTF-32 Unicode
                            if (text.Length > i + 9)
                            {
                                chars[index] = GetUTF32(i + 2);
                                i += 9;
                                index += 1;
                                continue;
                            }
                            break;
                        case 92: // \ escape
                            if (text.Length <= i + 2) break;
                            chars[index] = text[i + 1];
                            chars[index + 1] = text[i + 2];
                            i += 2;
                            index += 2;
                            continue;
                        case 110: // \n LineFeed
                            chars[index] = (char)10;
                            i += 1;
                            index += 1;
                            continue;
                        case 114: // \r
                            chars[index] = (char)13;
                            i += 1;
                            index += 1;
                            continue;
                        case 116: // \t Tab
                            chars[index] = (char)9;
                            i += 1;
                            index += 1;
                            continue;
                        case 117: // \u0000 for UTF-16 Unicode
                            if (text.Length > i + 5)
                            {
                                chars[index] = (char)GetUTF16(i + 2);
                                i += 5;
                                index += 1;
                                continue;
                            }
                            break;
                    }
                }

                // Handle UTF-32 in the input text (string).
                if (char.IsHighSurrogate(text[i]) && char.IsLowSurrogate(text[i + 1]))
                {
                    chars[index] = char.ConvertToUtf32(text[i], text[i + 1]);
                    i += 1;
                    index += 1;
                    continue;
                }

                chars[index] = text[i];
                index += 1;
            }
            chars[index] = (char)0;
        }



        private readonly float[] k_Power = { 5e-1f, 5e-2f, 5e-3f, 5e-4f, 5e-5f, 5e-6f, 5e-7f, 5e-8f, 5e-9f, 5e-10f }; // Used by FormatText to enable rounding and avoid using Mathf.Pow.

        /// <summary>
        /// Function used in conjunction with SetText()
        /// </summary>
        /// <param name="number"></param>
        /// <param name="index"></param>
        /// <param name="precision"></param>
        protected void AddFloatToCharArray(float number, ref int index, int precision)
        {
            if (number < 0)
            {
                m_input_CharArray[index++] = '-';
                number = -number;
            }

            number += k_Power[Mathf.Min(9, precision)];

            int integer = (int)number;
            AddIntToCharArray(integer, ref index, precision);

            if (precision > 0)
            {
                // Add the decimal point
                m_input_CharArray[index++] = '.';

                number -= integer;
                for (int p = 0; p < precision; p++)
                {
                    number *= 10;
                    int d = (int)(number);

                    m_input_CharArray[index++] = (char)(d + 48);
                    number -= d;
                }
            }
        }


        /// <summary>
        /// // Function used in conjunction with SetText()
        /// </summary>
        /// <param name="number"></param>
        /// <param name="index"></param>
        /// <param name="precision"></param>
        protected void AddIntToCharArray(int number, ref int index, int precision)
        {
            if (number < 0)
            {
                m_input_CharArray[index++] = '-';
                number = -number;
            }

            int i = index;
            do
            {
                m_input_CharArray[i++] = (char)(number % 10 + 48);
                number /= 10;
            } while (number > 0);

            int lastIndex = i;

            // Reverse string
            while (index + 1 < i)
            {
                i -= 1;
                char t = m_input_CharArray[index];
                m_input_CharArray[index] = m_input_CharArray[i];
                m_input_CharArray[i] = t;
                index += 1;
            }
            index = lastIndex;
        }


        /// <summary>
        /// Method used to determine the number of visible characters and required buffer allocations.
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        protected virtual int SetArraySizes(int[] chars) { return 0; }


        /// <summary>
        /// Method to parse the input text based on its source
        /// </summary>
        protected void ParseInputText()
        {
            //Debug.Log("Re-parsing Text.");
            ////Profiler.BeginSample("ParseInputText()");

            m_isInputParsingRequired = false;

            switch (m_inputSource)
            {
                case TextInputSources.Text:
                    StringToCharArray(m_text, ref m_char_buffer);
                    //isTextChanged = false;
                    break;
                case TextInputSources.SetText:
                    SetTextArrayToCharArray(m_input_CharArray, ref m_char_buffer);
                    //isSetTextChanged = false;
                    break;
                case TextInputSources.SetCharArray:
                    break;
            }

            //TMPro_EventManager.ON_TEXT_CHANGED(this);
            SetArraySizes(m_char_buffer);
            ////Profiler.EndSample();
        }


        /// <summary>
        /// Method which parses the text input, does the layout of the text as well as generating the geometry.
        /// </summary>
        protected virtual void GenerateTextMesh() { }


        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPreferredValues()
        {
            if (m_isInputParsingRequired || m_isTextTruncated)
                ParseInputText();

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth();

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight();

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object given the provided width and height.
        /// </summary>
        /// <returns></returns>
        public Vector2 GetPreferredValues(float width, float height)
        {
            if (m_isInputParsingRequired || m_isTextTruncated)
                ParseInputText();

            Vector2 margin = new Vector2(width, height);

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth(margin);

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        /// Function to Calculate the Preferred Width and Height of the text object given a certain string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 GetPreferredValues(string text)
        {
            StringToCharArray(text, ref m_char_buffer);
            SetArraySizes(m_char_buffer);

            Vector2 margin = new Vector2(Mathf.Infinity, Mathf.Infinity);

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth(margin);

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        ///  Function to Calculate the Preferred Width and Height of the text object given a certain string and size of text container.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public Vector2 GetPreferredValues(string text, float width, float height)
        {
            StringToCharArray(text, ref m_char_buffer);
            SetArraySizes(m_char_buffer);

            Vector2 margin = new Vector2(width, height);

            // CALCULATE PREFERRED WIDTH
            float preferredWidth = GetPreferredWidth(margin);

            // CALCULATE PREFERRED HEIGHT
            float preferredHeight = GetPreferredHeight(margin);

            return new Vector2(preferredWidth, preferredHeight);
        }


        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <returns></returns>
        protected float GetPreferredWidth()
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            // Set Margins to Infinity
            Vector2 margin = new Vector2(Mathf.Infinity, Mathf.Infinity);

            if (m_isInputParsingRequired || m_isTextTruncated)
                ParseInputText();

            float preferredWidth = CalculatePreferredValues(fontSize, margin).x;

            //Debug.Log("GetPreferredWidth() Called. Returning width of " + preferredWidth);

            return preferredWidth;
        }


        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <param name="margin"></param>
        /// <returns></returns>
        protected float GetPreferredWidth(Vector2 margin)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            float preferredWidth = CalculatePreferredValues(fontSize, margin).x;

            //Debug.Log("GetPreferredWidth() Called. Returning width of " + preferredWidth);

            return preferredWidth;
        }


        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <returns></returns>
        protected float GetPreferredHeight()
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            Vector2 margin = new Vector2(m_marginWidth != 0 ? m_marginWidth : Mathf.Infinity, Mathf.Infinity);

            if (m_isInputParsingRequired || m_isTextTruncated)
                ParseInputText();

            float preferredHeight = CalculatePreferredValues(fontSize, margin).y;

            //Debug.Log("GetPreferredHeight() Called. Returning height of " + preferredHeight);

            return preferredHeight;
        }


        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <param name="margin"></param>
        /// <returns></returns>
        protected float GetPreferredHeight(Vector2 margin)
        {
            float fontSize = m_enableAutoSizing ? m_fontSizeMax : m_fontSize;

            float preferredHeight = CalculatePreferredValues(fontSize, margin).y;

            //Debug.Log("GetPreferredHeight() Called. Returning height of " + preferredHeight);

            return preferredHeight;
        }


        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize)
        {
            //Debug.Log("*** CalculatePreferredValues() ***"); // ***** Frame: " + Time.frameCount);

            ////Profiler.BeginSample("TMP Generate Text - Phase I");

            // Early exit if no font asset was assigned. This should not be needed since Arial SDF will be assigned by default.
            if (m_fontAsset == null || m_fontAsset.characterDictionary == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned to Object ID: " + this.GetInstanceID());

                return Vector2.zero;
            }

            // Early exit if we don't have any Text to generate.
            if (m_char_buffer == null || m_char_buffer.Length == 0 || m_char_buffer[0] == (char)0)
            {
                return Vector2.zero;
            }

            m_currentFontAsset = m_fontAsset;
            m_currentMaterial = m_sharedMaterial;
            m_currentMaterialIndex = 0;
            m_materialReferenceStack.SetDefault(new MaterialReference(0, m_currentFontAsset, null, m_currentMaterial, m_padding));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_totalCharacterCount; // m_VisibleCharacters.Count;

            if (m_internalCharacterInfo == null || totalCharacterCount > m_internalCharacterInfo.Length)
            {
                m_internalCharacterInfo = new TMP_CharacterInfo[totalCharacterCount > 1024 ? totalCharacterCount + 256 : Mathf.NextPowerOfTwo(totalCharacterCount)];
            }

            // Calculate the scale of the font based on selected font size and sampling point size.
            m_fontScale = (defaultFontSize / m_currentFontAsset.fontInfo.PointSize * (m_isOrthographic ? 1 : 0.1f));
            m_fontScaleMultiplier = 1;

            // baseScale is calculated based on the font asset assigned to the text object.
            float baseScale = (defaultFontSize / m_fontAsset.fontInfo.PointSize * m_fontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
            float currentElementScale = m_fontScale;

            m_currentFontSize = defaultFontSize;
            m_sizeStack.SetDefault(m_currentFontSize);

            int charCode = 0; // Holds the character code of the currently being processed character.

            m_style = m_fontStyle; // Set the default style.

            float bold_xAdvance_multiplier = 1; // Used to increase spacing between character when style is bold.

            m_baselineOffset = 0; // Used by subscript characters.


            m_styleStack.Clear();

            m_lineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_lineHeight = 0;
            float lineGap = m_currentFontAsset.fontInfo.LineHeight - (m_currentFontAsset.fontInfo.Ascender - m_currentFontAsset.fontInfo.Descender);

            m_cSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_monoSpacing = 0;
            float lineOffsetDelta = 0;
            m_xAdvance = 0; // Used to track the position of each character.
            float maxXAdvance = 0; // Used to determine Preferred Width.

            tag_LineIndent = 0; // Used for indentation of text.
            tag_Indent = 0;
            m_indentStack.SetDefault(0);
            tag_NoParsing = false;
            //m_isIgnoringAlignment = false;

            m_characterCount = 0; // Total characters in the char[]


            // Tracking of line information
            m_firstCharacterOfLine = 0;
            m_maxLineAscender = -Mathf.Infinity;
            m_maxLineDescender = Mathf.Infinity;
            m_lineNumber = 0;

            float marginWidth = marginSize.x;
            //float marginHeight = marginSize.y;
            m_marginLeft = 0;
            m_marginRight = 0;
            m_width = -1;

            // Used by Unity's Auto Layout system.
            float renderedWidth = 0;
            float renderedHeight = 0;

            // Tracking of the highest Ascender
            m_maxAscender = 0;
            m_maxDescender = 0;


            // Initialize struct to track states of word wrapping
            bool isFirstWord = true;
            bool isLastBreakingChar = false;
            WordWrapState savedLineState = new WordWrapState();
            SaveWordWrappingState(ref savedLineState, 0, 0);
            WordWrapState savedWordWrapState = new WordWrapState();
            int wrappingIndex = 0;


            //int loopCountA = 0;

            int endTagIndex = 0;
            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; m_char_buffer[i] != 0; i++)
            {
                charCode = m_char_buffer[i];
                m_textElementType = TMP_TextElementType.Character;

                m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;
                m_currentFontAsset = m_materialReferences[m_currentMaterialIndex].fontAsset;

                int prev_MaterialIndex = m_currentMaterialIndex;

                // Parse Rich Text Tag
                #region Parse Rich Text Tag
                if (m_isRichText && charCode == 60)  // '<'
                {
                    m_isParsingText = true;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_char_buffer, i + 1, out endTagIndex))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        if (m_textElementType == TMP_TextElementType.Character)
                            continue;
                    }
                }
                #endregion End Parse Rich Text Tag

                m_isParsingText = false;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

                if (m_textElementType == TMP_TextElementType.Character)
                {
                    if ((m_style & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)charCode))
                            charCode = char.ToUpper((char)charCode);

                    }
                    else if ((m_style & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)charCode))
                            charCode = char.ToLower((char)charCode);
                    }
                    else if ((m_fontStyle & FontStyles.SmallCaps) == FontStyles.SmallCaps || (m_style & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        if (char.IsLower((char)charCode))
                        {
                            smallCapsMultiplier = 0.8f;
                            charCode = char.ToUpper((char)charCode);
                        }
                    }
                }
                #endregion


                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data
                if (m_textElementType == TMP_TextElementType.Sprite)
                {
                    TMP_Sprite sprite = m_currentSpriteAsset.spriteInfoList[m_spriteIndex];
                    if (sprite == null) continue;

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    charCode = 57344 + m_spriteIndex;

                    m_cached_TextElement = sprite;

                    // Adjust the offset relative to the pivot point.
                    //sprite.xOffset += sprite.pivot.x;
                    //sprite.yOffset += sprite.pivot.y;

                    currentElementScale = m_fontAsset.fontInfo.Ascender / sprite.height * sprite.scale * baseScale;

                    m_internalCharacterInfo[m_characterCount].elementType = TMP_TextElementType.Sprite;

                    m_currentMaterialIndex = prev_MaterialIndex;
                }
                else if (m_textElementType == TMP_TextElementType.Character)
                {
                    m_cached_TextElement = m_textInfo.characterInfo[m_characterCount].textElement;
                    m_currentFontAsset = m_textInfo.characterInfo[m_characterCount].fontAsset;

                    m_currentMaterialIndex = m_textInfo.characterInfo[m_characterCount].materialReferenceIndex;

                    // Re-calculate font scale as the font asset may have changed.
                    m_fontScale = m_currentFontSize * smallCapsMultiplier / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f); 

                    currentElementScale = m_fontScale * m_fontScaleMultiplier;

                    m_internalCharacterInfo[m_characterCount].elementType = TMP_TextElementType.Character;

                    //padding = m_currentMaterialIndex == 0 ? m_padding : m_subTextObjects[m_currentMaterialIndex].padding;
                }
                #endregion


                // Store some of the text object's information
                m_internalCharacterInfo[m_characterCount].character = (char)charCode;


                // Handle Kerning if Enabled.
                #region Handle Kerning
                if (m_enableKerning && m_characterCount >= 1)
                {
                    int prev_charCode = m_internalCharacterInfo[m_characterCount - 1].character;
                    KerningPairKey keyValue = new KerningPairKey(prev_charCode, charCode);

                    KerningPair pair;

                    m_currentFontAsset.kerningDictionary.TryGetValue(keyValue.key, out pair);
                    if (pair != null)
                    {
                        m_xAdvance += pair.XadvanceOffset * currentElementScale;
                    }
                }
                #endregion


                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_monoSpacing != 0)
                {
                    monoAdvance = (m_monoSpacing / 2 - (m_cached_TextElement.width / 2 + m_cached_TextElement.xOffset) * currentElementScale);
                    m_xAdvance += monoAdvance;
                }
                #endregion


                // Set Padding based on selected font style
                #region Handle Style Padding
                if ((m_style & FontStyles.Bold) == FontStyles.Bold || (m_fontStyle & FontStyles.Bold) == FontStyles.Bold) // Checks for any combination of Bold Style.
                {
                    //style_padding = m_currentFontAsset.boldStyle * 2;
                    bold_xAdvance_multiplier = 1 + m_currentFontAsset.boldSpacing * 0.01f;
                }
                else
                {
                    //style_padding = m_currentFontAsset.normalStyle * 2;
                    bold_xAdvance_multiplier = 1.0f;
                }
                #endregion Handle Style Padding

                //m_internalTextInfo.characterInfo[m_characterCount].scale = currentElementScale;
                //m_internalTextInfo.characterInfo[m_characterCount].origin = m_xAdvance;
                m_internalCharacterInfo[m_characterCount].baseLine = 0 - m_lineOffset + m_baselineOffset;


                // Compute and save text element Ascender and maximum line Ascender.
                float elementAscender = m_currentFontAsset.fontInfo.Ascender * (m_textElementType == TMP_TextElementType.Character ? currentElementScale : baseScale) + m_baselineOffset;
                /* float elementAscenderII = */
                m_internalCharacterInfo[m_characterCount].ascender = elementAscender - m_lineOffset;
                m_maxLineAscender = elementAscender > m_maxLineAscender ? elementAscender : m_maxLineAscender;

                // Compute and save text element Descender and maximum line Descender.
                float elementDescender = m_currentFontAsset.fontInfo.Descender * (m_textElementType == TMP_TextElementType.Character ? currentElementScale : baseScale) + m_baselineOffset;
                float elementDescenderII = m_internalCharacterInfo[m_characterCount].descender = elementDescender - m_lineOffset;
                m_maxLineDescender = elementDescender < m_maxLineDescender ? elementDescender : m_maxLineDescender;

                // Adjust maxLineAscender and maxLineDescender if style is superscript or subscript
                if ((m_style & FontStyles.Subscript) == FontStyles.Subscript || (m_style & FontStyles.Superscript) == FontStyles.Superscript)
                {
                    float baseAscender = (elementAscender - m_baselineOffset) / m_currentFontAsset.fontInfo.SubSize;
                    elementAscender = m_maxLineAscender;
                    m_maxLineAscender = baseAscender > m_maxLineAscender ? baseAscender : m_maxLineAscender;

                    float baseDescender = (elementDescender - m_baselineOffset) / m_currentFontAsset.fontInfo.SubSize;
                    elementDescender = m_maxLineDescender;
                    m_maxLineDescender = baseDescender < m_maxLineDescender ? baseDescender : m_maxLineDescender;
                }

                if (m_lineNumber == 0) m_maxAscender = m_maxAscender > elementAscender ? m_maxAscender : elementAscender;
                //if (m_lineOffset == 0) pageAscender = pageAscender > elementAscender ? pageAscender : elementAscender;


                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == 9 || !char.IsWhiteSpace((char)charCode) || m_textElementType == TMP_TextElementType.Sprite)
                {
                    //m_internalTextInfo.characterInfo[m_characterCount].isVisible = true;

                    // Check if Character exceeds the width of the Text Container
                    #region Handle Line Breaking, Text Auto-Sizing and Horizontal Overflow
                    float width = m_width != -1 ? Mathf.Min(marginWidth + 0.0001f - m_marginLeft - m_marginRight, m_width) : marginWidth + 0.0001f - m_marginLeft - m_marginRight;

                    //m_internalTextInfo.lineInfo[m_lineNumber].width = width;
                    //m_internalTextInfo.lineInfo[m_lineNumber].marginLeft = m_marginLeft;

                    if (m_xAdvance + m_cached_TextElement.xAdvance * currentElementScale > width)
                    {
                        // Word Wrapping
                        #region Handle Word Wrapping
                        if (enableWordWrapping && m_characterCount != m_firstCharacterOfLine)
                        {
                            // Check if word wrapping is still possible
                            #region Line Breaking Check
                            if (wrappingIndex == savedWordWrapState.previous_WordBreak || isFirstWord)
                            {
                                // Word wrapping is no longer possible, now breaking up individual words.
                                if (m_isCharacterWrappingEnabled == false)
                                {
                                    m_isCharacterWrappingEnabled = true;
                                }
                                else
                                    isLastBreakingChar = true;

                                //m_recursiveCount += 1;
                                //if (m_recursiveCount > 20)
                                //{
                                    //Debug.Log("Recursive count exceeded!");
                                    //continue;
                                //}
                            }
                            #endregion

                            // Restore to previously stored state of last valid (space character or linefeed)
                            i = RestoreWordWrappingState(ref savedWordWrapState);
                            wrappingIndex = i;  // Used to detect when line length can no longer be reduced.


                            // Check if Line Spacing of previous line needs to be adjusted.
                            if (m_lineNumber > 0 && !TMP_Math.Approximately(m_maxLineAscender, m_startOfLineAscender) && m_lineHeight == 0)
                            {
                                //Debug.Log("(1) Adjusting Line Spacing on line #" + m_lineNumber);
                                float offsetDelta = m_maxLineAscender - m_startOfLineAscender;
                                AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, offsetDelta);
                                m_lineOffset += offsetDelta;
                                savedWordWrapState.lineOffset = m_lineOffset;
                                savedWordWrapState.previousLineAscender = m_maxLineAscender;

                                // TODO - Add check for character exceeding vertical bounds
                            }
                            //m_isNewPage = false;

                            // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                            float lineAscender = m_maxLineAscender - m_lineOffset;
                            float lineDescender = m_maxLineDescender - m_lineOffset;


                            // Update maxDescender and maxVisibleDescender
                            m_maxDescender = m_maxDescender < lineDescender ? m_maxDescender : lineDescender;


                            m_firstCharacterOfLine = m_characterCount; // Store first character of the next line.

                            // Compute Preferred Width & Height
                            renderedWidth += m_xAdvance;
                            if (m_enableWordWrapping)
                                renderedHeight = m_maxAscender - m_maxDescender;
                            else
                                renderedHeight = Mathf.Max(renderedHeight, lineAscender - lineDescender);


                            // Store the state of the line before starting on the new line.
                            SaveWordWrappingState(ref savedLineState, i, m_characterCount - 1);

                            m_lineNumber += 1;
                            //isStartOfNewLine = true;

                            // Check to make sure Array is large enough to hold a new line.
                            //if (m_lineNumber >= m_internalTextInfo.lineInfo.Length)
                            //    ResizeLineExtents(m_lineNumber);

                            // Apply Line Spacing based on scale of the last character of the line.
                            if (m_lineHeight == 0)
                            {
                                float ascender = m_internalCharacterInfo[m_characterCount].ascender - m_internalCharacterInfo[m_characterCount].baseLine;
                                lineOffsetDelta = 0 - m_maxLineDescender + ascender + (lineGap + m_lineSpacing + m_lineSpacingDelta) * baseScale;
                                m_lineOffset += lineOffsetDelta;

                                m_startOfLineAscender = ascender;
                            }
                            else
                                m_lineOffset += m_lineHeight + m_lineSpacing * baseScale;

                            m_maxLineAscender = -Mathf.Infinity;
                            m_maxLineDescender = Mathf.Infinity;

                            m_xAdvance = 0 + tag_Indent;

                            continue;
                        }
                        #endregion End Word Wrapping
                    }
                    #endregion End Check for Characters Exceeding Width of Text Container

                }
                #endregion Handle Visible Characters


                // Check if Line Spacing of previous line needs to be adjusted.
                #region Adjust Line Spacing
                if (m_lineNumber > 0 && !TMP_Math.Approximately(m_maxLineAscender, m_startOfLineAscender) && m_lineHeight == 0 && !m_isNewPage)
                {
                    //Debug.Log("Inline - Adjusting Line Spacing on line #" + m_lineNumber);
                    //float gap = 0; // Compute gap.

                    float offsetDelta = m_maxLineAscender - m_startOfLineAscender;
                    AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, offsetDelta);
                    elementDescenderII -= offsetDelta;
                    m_lineOffset += offsetDelta;

                    m_startOfLineAscender += offsetDelta;
                    savedWordWrapState.lineOffset = m_lineOffset;
                    savedWordWrapState.previousLineAscender = m_startOfLineAscender;
                }
                #endregion


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops
                if (charCode == 9)
                {
                    m_xAdvance += m_currentFontAsset.fontInfo.TabWidth * currentElementScale;
                }
                else if (m_monoSpacing != 0)
                    m_xAdvance += (m_monoSpacing - monoAdvance + ((m_characterSpacing + m_currentFontAsset.normalSpacingOffset) * currentElementScale) + m_cSpacing);
                else
                {
                    m_xAdvance += ((m_cached_TextElement.xAdvance * bold_xAdvance_multiplier + m_characterSpacing + m_currentFontAsset.normalSpacingOffset) * currentElementScale + m_cSpacing);
                }


                // Store xAdvance information
                //m_internalTextInfo.characterInfo[m_characterCount].xAdvance = m_xAdvance;

                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == 13)
                {
                    maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + m_xAdvance);
                    renderedWidth = 0;
                    m_xAdvance = 0 + tag_Indent;
                }
                #endregion Carriage Return


                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character
                if (charCode == 10 || m_characterCount == totalCharacterCount - 1)
                {
                    // Check if Line Spacing of previous line needs to be adjusted.
                    if (m_lineNumber > 0 && !TMP_Math.Approximately(m_maxLineAscender, m_startOfLineAscender) && m_lineHeight == 0)
                    {
                        //Debug.Log("(2) Adjusting Line Spacing on line #" + m_lineNumber);
                        float offsetDelta = m_maxLineAscender - m_startOfLineAscender;
                        AdjustLineOffset(m_firstCharacterOfLine, m_characterCount, offsetDelta);
                        elementDescenderII -= offsetDelta;
                        m_lineOffset += offsetDelta;
                    }

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    //float lineAscender = m_maxLineAscender - m_lineOffset;
                    float lineDescender = m_maxLineDescender - m_lineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_maxDescender = m_maxDescender < lineDescender ? m_maxDescender : lineDescender;

                    m_firstCharacterOfLine = m_characterCount + 1;

                    // Store PreferredWidth paying attention to linefeed and last character of text.
                    if (charCode == 10 && m_characterCount != totalCharacterCount - 1)
                    {
                        maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + m_xAdvance);
                        renderedWidth = 0;
                    }
                    else
                        renderedWidth = Mathf.Max(maxXAdvance, renderedWidth + m_xAdvance);

                    renderedHeight = m_maxAscender - m_maxDescender;


                    // Add new line if not last lines or character.
                    if (charCode == 10)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref savedLineState, i, m_characterCount);
                        // Store the state of the last Character before the new line.
                        SaveWordWrappingState(ref savedWordWrapState, i, m_characterCount);

                        m_lineNumber += 1;

                        // Apply Line Spacing
                        if (m_lineHeight == 0)
                        {
                            lineOffsetDelta = 0 - m_maxLineDescender + elementAscender + (lineGap + m_lineSpacing + m_paragraphSpacing + m_lineSpacingDelta) * baseScale;
                            m_lineOffset += lineOffsetDelta;
                        }
                        else
                            m_lineOffset += m_lineHeight + (m_lineSpacing + m_paragraphSpacing) * baseScale;

                        m_maxLineAscender = -Mathf.Infinity;
                        m_maxLineDescender = Mathf.Infinity;
                        m_startOfLineAscender = elementAscender;

                        m_xAdvance = 0 + tag_LineIndent + tag_Indent;
                    }
                }
                #endregion Check for Linefeed or Last Character


                // Save State of Mesh Creation for handling of Word Wrapping
                #region Save Word Wrapping State
                if (m_enableWordWrapping || m_overflowMode == TextOverflowModes.Truncate || m_overflowMode == TextOverflowModes.Ellipsis)
                {
                    if ((charCode == 9 || charCode == 32) && !m_isNonBreakingSpace)
                    {
                        // We store the state of numerous variables for the most recent Space, LineFeed or Carriage Return to enable them to be restored 
                        // for Word Wrapping.
                        SaveWordWrappingState(ref savedWordWrapState, i, m_characterCount);
                        m_isCharacterWrappingEnabled = false;
                        isFirstWord = false;
                    }
                    else if (charCode > 0x2e80 && charCode < 0x9fff)
                    {
                        if (m_currentFontAsset.lineBreakingInfo.leadingCharacters.ContainsKey(charCode) == false &&
                           (m_characterCount < totalCharacterCount - 1 &&
                            m_currentFontAsset.lineBreakingInfo.followingCharacters.ContainsKey(m_internalCharacterInfo[m_characterCount + 1].character) == false))
                        {
                            SaveWordWrappingState(ref savedWordWrapState, i, m_characterCount);
                            m_isCharacterWrappingEnabled = false;
                            isFirstWord = false;

                        }
                    }
                    else if ((isFirstWord || m_isCharacterWrappingEnabled == true || isLastBreakingChar))
                        SaveWordWrappingState(ref savedWordWrapState, i, m_characterCount);
                }
                #endregion Save Word Wrapping State

                m_characterCount += 1;
            }


            m_isCharacterWrappingEnabled = false;

            // Adjust Preferred Width and Height to account for Margins.
            renderedWidth += m_margin.x > 0 ? m_margin.x : 0;
            renderedWidth += m_margin.z > 0 ? m_margin.z : 0;

            renderedHeight += m_margin.y > 0 ? m_margin.y : 0;
            renderedHeight += m_margin.w > 0 ? m_margin.w : 0;

            ////Profiler.EndSample();

            return new Vector2(renderedWidth, renderedHeight);
        }



        /// <summary>
        /// Method to adjust line spacing as a result of using different fonts or font point size.
        /// </summary>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="offset"></param>
        protected virtual void AdjustLineOffset(int startIndex, int endIndex, float offset) { }


        /// <summary>
        /// Function to increase the size of the Line Extents Array.
        /// </summary>
        /// <param name="size"></param>
        protected void ResizeLineExtents(int size)
        {
            size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size + 1);

            TMP_LineInfo[] temp_lineInfo = new TMP_LineInfo[size];
            for (int i = 0; i < size; i++)
            {
                if (i < m_textInfo.lineInfo.Length)
                    temp_lineInfo[i] = m_textInfo.lineInfo[i];
                else
                {
                    temp_lineInfo[i].lineExtents.min = k_InfinityVectorPositive;
                    temp_lineInfo[i].lineExtents.max = k_InfinityVectorNegative;

                    temp_lineInfo[i].ascender = k_InfinityVectorNegative.x;
                    temp_lineInfo[i].descender = k_InfinityVectorPositive.x;
                }
            }

            m_textInfo.lineInfo = temp_lineInfo;
        }
        protected static Vector2 k_InfinityVectorPositive = new Vector2(1000000, 1000000);
        protected static Vector2 k_InfinityVectorNegative = new Vector2(-1000000, -1000000);


        /// <summary>
        /// Function used to evaluate the length of a text string.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public virtual TMP_TextInfo GetTextInfo(string text) { return null; }


        /// <summary>
        /// Function to force an update of the margin size.
        /// </summary>
        protected virtual void ComputeMarginSize() { }


        /// <summary>
        /// Function used in conjunction with GetTextInfo to figure out Array allocations.
        /// </summary>
        /// <param name="chars"></param>
        /// <returns></returns>
        protected int GetArraySizes(int[] chars)
        {
            //Debug.Log("Set Array Size called.");

            //int visibleCount = 0;
            //int totalCount = 0;
            int tagEnd = 0;

            m_totalCharacterCount = 0;
            m_isUsingBold = false;
            m_isParsingText = false;


            //m_VisibleCharacters.Clear();

            for (int i = 0; chars[i] != 0; i++)
            {
                int c = chars[i];

                if (m_isRichText && c == 60) // if Char '<'
                {
                    // Check if Tag is Valid
                    if (ValidateHtmlTag(chars, i + 1, out tagEnd))
                    {
                        i = tagEnd;
                        //if ((m_style & FontStyles.Underline) == FontStyles.Underline) visibleCount += 3;

                        if ((m_style & FontStyles.Bold) == FontStyles.Bold) m_isUsingBold = true;

                        continue;
                    }
                }

                if (!char.IsWhiteSpace((char)c))
                {
                    //visibleCount += 1;
                }

                //m_VisibleCharacters.Add((char)c);
                m_totalCharacterCount += 1;
            }

            return m_totalCharacterCount;
        }


        /// <summary>
        /// Save the State of various variables used in the mesh creation loop in conjunction with Word Wrapping
        /// </summary>
        /// <param name="state"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        protected void SaveWordWrappingState(ref WordWrapState state, int index, int count)
        {
            // Multi Font & Material support related
            state.currentFontAsset = m_currentFontAsset;
            state.currentSpriteAsset = m_currentSpriteAsset;
            state.currentMaterial = m_currentMaterial;
            state.currentMaterialIndex = m_currentMaterialIndex;

            state.previous_WordBreak = index;
            state.total_CharacterCount = count;
            state.visible_CharacterCount = m_visibleCharacterCount;
            state.visible_SpriteCount = m_visibleSpriteCount;
            state.visible_LinkCount = m_textInfo.linkCount;

            state.firstCharacterIndex = m_firstCharacterOfLine;
            state.firstVisibleCharacterIndex = m_firstVisibleCharacterOfLine;
            state.lastVisibleCharIndex = m_lastVisibleCharacterOfLine;

            state.fontStyle = m_style;
            state.fontScale = m_fontScale;
            //state.maxFontScale = m_maxFontScale;
            state.fontScaleMultiplier = m_fontScaleMultiplier;
            state.currentFontSize = m_currentFontSize;

            state.xAdvance = m_xAdvance;
            state.maxAscender = m_maxAscender;
            state.maxDescender = m_maxDescender;
            state.maxLineAscender = m_maxLineAscender;
            state.maxLineDescender = m_maxLineDescender;
            state.previousLineAscender = m_startOfLineAscender;
            state.preferredWidth = m_preferredWidth;
            state.preferredHeight = m_preferredHeight;
            state.meshExtents = m_meshExtents;

            state.lineNumber = m_lineNumber;
            state.lineOffset = m_lineOffset;
            state.baselineOffset = m_baselineOffset;

            //state.alignment = m_lineJustification;
            state.vertexColor = m_htmlColor;
            state.tagNoParsing = tag_NoParsing;

            // XML Tag Stack
            state.colorStack = m_colorStack;
            state.sizeStack = m_sizeStack;
            state.fontWeightStack = m_fontWeightStack;
            state.styleStack = m_styleStack;
            state.actionStack = m_actionStack;
            state.materialReferenceStack = m_materialReferenceStack;

            if (m_lineNumber < m_textInfo.lineInfo.Length)
                state.lineInfo = m_textInfo.lineInfo[m_lineNumber];
        }


        /// <summary>
        /// Restore the State of various variables used in the mesh creation loop.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        protected int RestoreWordWrappingState(ref WordWrapState state)
        {
            int index = state.previous_WordBreak;

            // Multi Font & Material support related
            m_currentFontAsset = state.currentFontAsset;
            m_currentSpriteAsset = state.currentSpriteAsset;
            m_currentMaterial = state.currentMaterial;
            m_currentMaterialIndex = state.currentMaterialIndex;

            m_characterCount = state.total_CharacterCount + 1;
            m_visibleCharacterCount = state.visible_CharacterCount;
            m_visibleSpriteCount = state.visible_SpriteCount;
            m_textInfo.linkCount = state.visible_LinkCount;

            m_firstCharacterOfLine = state.firstCharacterIndex;
            m_firstVisibleCharacterOfLine = state.firstVisibleCharacterIndex;
            m_lastVisibleCharacterOfLine = state.lastVisibleCharIndex;

            m_style = state.fontStyle;
            m_fontScale = state.fontScale;
            m_fontScaleMultiplier = state.fontScaleMultiplier;
            //m_maxFontScale = state.maxFontScale;
            m_currentFontSize = state.currentFontSize;

            m_xAdvance = state.xAdvance;
            m_maxAscender = state.maxAscender;
            m_maxDescender = state.maxDescender;
            m_maxLineAscender = state.maxLineAscender;
            m_maxLineDescender = state.maxLineDescender;
            m_startOfLineAscender = state.previousLineAscender;
            m_preferredWidth = state.preferredWidth;
            m_preferredHeight = state.preferredHeight;
            m_meshExtents = state.meshExtents;

            m_lineNumber = state.lineNumber;
            m_lineOffset = state.lineOffset;
            m_baselineOffset = state.baselineOffset;

            //m_lineJustification = state.alignment;
            m_htmlColor = state.vertexColor;
            tag_NoParsing = state.tagNoParsing;

            // XML Tag Stack
            m_colorStack = state.colorStack;
            m_sizeStack = state.sizeStack;
            m_fontWeightStack = state.fontWeightStack;
            m_styleStack = state.styleStack;
            m_actionStack = state.actionStack;
            m_materialReferenceStack = state.materialReferenceStack;

            if (m_lineNumber < m_textInfo.lineInfo.Length)
                m_textInfo.lineInfo[m_lineNumber] = state.lineInfo;

            return index;
        }


        /// <summary>
        /// Store vertex information for each character.
        /// </summary>
        /// <param name="style_padding">Style_padding.</param>
        /// <param name="vertexColor">Vertex color.</param>
        protected virtual void SaveGlyphVertexInfo(float padding, float style_padding, Color32 vertexColor)
        {
            // Save the Vertex Position for the Character
            #region Setup Mesh Vertices
            m_textInfo.characterInfo[m_characterCount].vertex_BL.position = m_textInfo.characterInfo[m_characterCount].bottomLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.position = m_textInfo.characterInfo[m_characterCount].topLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.position = m_textInfo.characterInfo[m_characterCount].topRight;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.position = m_textInfo.characterInfo[m_characterCount].bottomRight;
            #endregion


            #region Setup Vertex Colors
            // Alpha is the lower of the vertex color or tag color alpha used.
            vertexColor.a = m_fontColor32.a < vertexColor.a ? (byte)(m_fontColor32.a) : (byte)(vertexColor.a);

            // Handle Vertex Colors & Vertex Color Gradient
            if (!m_enableVertexGradient)
            {
                m_textInfo.characterInfo[m_characterCount].vertex_BL.color = vertexColor;
                m_textInfo.characterInfo[m_characterCount].vertex_TL.color = vertexColor;
                m_textInfo.characterInfo[m_characterCount].vertex_TR.color = vertexColor;
                m_textInfo.characterInfo[m_characterCount].vertex_BR.color = vertexColor;
            }
            else
            {
                if (!m_overrideHtmlColors && !m_htmlColor.CompareRGB(m_fontColor32))
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color = vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color = vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color = vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color = vertexColor;
                }
                else // Handle Vertex Color Gradient
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color = m_fontColorGradient.bottomLeft * vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color = m_fontColorGradient.topLeft * vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color = m_fontColorGradient.topRight * vertexColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color = m_fontColorGradient.bottomRight * vertexColor;
                }
            }
            #endregion

            // Apply style_padding only if this is a SDF Shader.
            if (!m_isSDFShader)
                style_padding = 0;


            // Setup UVs for the Character
            #region Setup UVs
            FaceInfo faceInfo = m_currentFontAsset.fontInfo;

            Vector2 uv0;
            uv0.x = (m_cached_TextElement.x - padding - style_padding) / faceInfo.AtlasWidth;
            uv0.y = 1 - (m_cached_TextElement.y + padding + style_padding + m_cached_TextElement.height) / faceInfo.AtlasHeight;

            Vector2 uv1;
            uv1.x = uv0.x;
            uv1.y = 1 - (m_cached_TextElement.y - padding - style_padding) / faceInfo.AtlasHeight;

            Vector2 uv2;
            uv2.x = (m_cached_TextElement.x + padding + style_padding + m_cached_TextElement.width) / faceInfo.AtlasWidth;
            uv2.y = uv1.y;

            Vector2 uv3;
            uv3.x = uv2.x;
            uv3.y = uv0.y;

            //Vector2 uv0 = new Vector2((m_cached_TextElement.x - padding - style_padding) / faceInfo.AtlasWidth, 1 - (m_cached_TextElement.y + padding + style_padding + m_cached_TextElement.height) / faceInfo.AtlasHeight);  // bottom left
            //Vector2 uv1 = new Vector2(uv0.x, 1 - (m_cached_TextElement.y - padding - style_padding) / faceInfo.AtlasHeight);  // top left
            //Vector2 uv2 = new Vector2((m_cached_TextElement.x + padding + style_padding + m_cached_TextElement.width) / faceInfo.AtlasWidth, uv1.y); // top right
            //Vector2 uv3 = new Vector2(uv2.x, uv0.y); // bottom right

            // Store UV Information
            m_textInfo.characterInfo[m_characterCount].vertex_BL.uv = uv0;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.uv = uv1;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.uv = uv2;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.uv = uv3;
            #endregion Setup UVs


            // Normal
            #region Setup Normals & Tangents
            //Vector3 normal = new Vector3(0, 0, -1);
            //m_textInfo.characterInfo[m_characterCount].vertex_BL.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_TL.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_TR.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_BR.normal = normal;

            // Tangents
            //Vector4 tangent = new Vector4(-1, 0, 0, 1);
            //m_textInfo.characterInfo[m_characterCount].vertex_BL.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_TL.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_TR.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_BR.tangent = tangent;
            #endregion end Normals & Tangents
        }


        /// <summary>
        /// Store vertex information for each sprite.
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="style_padding"></param>
        /// <param name="vertexColor"></param>
        protected virtual void SaveSpriteVertexInfo(Color32 vertexColor)
        {
            // Save the Vertex Position for the Character
            #region Setup Mesh Vertices
            m_textInfo.characterInfo[m_characterCount].vertex_BL.position = m_textInfo.characterInfo[m_characterCount].bottomLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.position = m_textInfo.characterInfo[m_characterCount].topLeft;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.position = m_textInfo.characterInfo[m_characterCount].topRight;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.position = m_textInfo.characterInfo[m_characterCount].bottomRight;
            #endregion

            // Vertex Color Alpha
            if (m_tintAllSprites) m_tintSprite = true;
            Color32 spriteColor = m_tintSprite ? m_spriteColor.Multiply(vertexColor) : m_spriteColor;
            spriteColor.a = spriteColor.a < m_fontColor32.a ? spriteColor.a = spriteColor.a < vertexColor.a ? spriteColor.a : vertexColor.a : m_fontColor32.a;

            // Handle Vertex Colors & Vertex Color Gradient
            if (!m_enableVertexGradient)
            {
                m_textInfo.characterInfo[m_characterCount].vertex_BL.color = spriteColor;
                m_textInfo.characterInfo[m_characterCount].vertex_TL.color = spriteColor;
                m_textInfo.characterInfo[m_characterCount].vertex_TR.color = spriteColor;
                m_textInfo.characterInfo[m_characterCount].vertex_BR.color = spriteColor;
            }
            else
            {
                if (!m_overrideHtmlColors && !m_htmlColor.CompareRGB(m_fontColor32))
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color = spriteColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color = spriteColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color = spriteColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color = spriteColor;
                }
                else // Handle Vertex Color Gradient
                {
                    m_textInfo.characterInfo[m_characterCount].vertex_BL.color = m_tintSprite ? spriteColor.Multiply(m_fontColorGradient.bottomLeft) : spriteColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TL.color = m_tintSprite ? spriteColor.Multiply(m_fontColorGradient.topLeft) : spriteColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_TR.color = m_tintSprite ? spriteColor.Multiply(m_fontColorGradient.topRight) : spriteColor;
                    m_textInfo.characterInfo[m_characterCount].vertex_BR.color = m_tintSprite ? spriteColor.Multiply(m_fontColorGradient.bottomRight) : spriteColor;
                }
            }

            // Setup UVs for the Character
            #region Setup UVs
            Vector2 uv0 = new Vector2(m_cached_TextElement.x / m_currentSpriteAsset.spriteSheet.width, m_cached_TextElement.y / m_currentSpriteAsset.spriteSheet.height);  // bottom left
            Vector2 uv1 = new Vector2(uv0.x, (m_cached_TextElement.y + m_cached_TextElement.height) / m_currentSpriteAsset.spriteSheet.height);  // top left
            Vector2 uv2 = new Vector2((m_cached_TextElement.x + m_cached_TextElement.width) / m_currentSpriteAsset.spriteSheet.width, uv1.y); // top right
            Vector2 uv3 = new Vector2(uv2.x, uv0.y); // bottom right

            // Store UV Information
            m_textInfo.characterInfo[m_characterCount].vertex_BL.uv = uv0;
            m_textInfo.characterInfo[m_characterCount].vertex_TL.uv = uv1;
            m_textInfo.characterInfo[m_characterCount].vertex_TR.uv = uv2;
            m_textInfo.characterInfo[m_characterCount].vertex_BR.uv = uv3;
            #endregion Setup UVs


            // Normal
            #region Setup Normals & Tangents
            //Vector3 normal = new Vector3(0, 0, -1);
            //m_textInfo.characterInfo[m_characterCount].vertex_BL.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_TL.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_TR.normal = normal;
            //m_textInfo.characterInfo[m_characterCount].vertex_BR.normal = normal;

            // Tangents
            //Vector4 tangent = new Vector4(-1, 0, 0, 1);
            //m_textInfo.characterInfo[m_characterCount].vertex_BL.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_TL.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_TR.tangent = tangent;
            //m_textInfo.characterInfo[m_characterCount].vertex_BR.tangent = tangent;
            #endregion end Normals & Tangents

        }


        /// <summary>
        /// Store vertex attributes into the appropriate TMP_MeshInfo.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="index_X4"></param>
        protected virtual void FillCharacterVertexBuffers(int i, int index_X4)
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            TMP_CharacterInfo[] characterInfoArray = m_textInfo.characterInfo;
            m_textInfo.characterInfo[i].vertexIndex = (short)(index_X4);

            // Setup Vertices for Characters
            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;


            // Setup UVS0
            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;


            // Setup UVS2
            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;


            // Setup UVS4
            //m_textInfo.meshInfo[0].uvs4[0 + index_X4] = characterInfoArray[i].vertex_BL.uv4;
            //m_textInfo.meshInfo[0].uvs4[1 + index_X4] = characterInfoArray[i].vertex_TL.uv4;
            //m_textInfo.meshInfo[0].uvs4[2 + index_X4] = characterInfoArray[i].vertex_TR.uv4;
            //m_textInfo.meshInfo[0].uvs4[3 + index_X4] = characterInfoArray[i].vertex_BR.uv4;


            // setup Vertex Colors
            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = characterInfoArray[i].vertex_BR.color;

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + 4;
        }


        /// <summary>
        /// Fill Vertex Buffers for Sprites
        /// </summary>
        /// <param name="i"></param>
        /// <param name="spriteIndex_X4"></param>
        protected virtual void FillSpriteVertexBuffers(int i, int index_X4)
        {
            int materialIndex = m_textInfo.characterInfo[i].materialReferenceIndex;
            index_X4 = m_textInfo.meshInfo[materialIndex].vertexCount;

            TMP_CharacterInfo[] characterInfoArray = m_textInfo.characterInfo;
            m_textInfo.characterInfo[i].vertexIndex = (short)(index_X4);

            // Setup Vertices for Characters
            m_textInfo.meshInfo[materialIndex].vertices[0 + index_X4] = characterInfoArray[i].vertex_BL.position;
            m_textInfo.meshInfo[materialIndex].vertices[1 + index_X4] = characterInfoArray[i].vertex_TL.position;
            m_textInfo.meshInfo[materialIndex].vertices[2 + index_X4] = characterInfoArray[i].vertex_TR.position;
            m_textInfo.meshInfo[materialIndex].vertices[3 + index_X4] = characterInfoArray[i].vertex_BR.position;


            // Setup UVS0
            m_textInfo.meshInfo[materialIndex].uvs0[0 + index_X4] = characterInfoArray[i].vertex_BL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[1 + index_X4] = characterInfoArray[i].vertex_TL.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[2 + index_X4] = characterInfoArray[i].vertex_TR.uv;
            m_textInfo.meshInfo[materialIndex].uvs0[3 + index_X4] = characterInfoArray[i].vertex_BR.uv;


            // Setup UVS2
            m_textInfo.meshInfo[materialIndex].uvs2[0 + index_X4] = characterInfoArray[i].vertex_BL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[1 + index_X4] = characterInfoArray[i].vertex_TL.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[2 + index_X4] = characterInfoArray[i].vertex_TR.uv2;
            m_textInfo.meshInfo[materialIndex].uvs2[3 + index_X4] = characterInfoArray[i].vertex_BR.uv2;


            // Setup UVS4
            //m_textInfo.meshInfo[0].uvs4[0 + index_X4] = characterInfoArray[i].vertex_BL.uv4;
            //m_textInfo.meshInfo[0].uvs4[1 + index_X4] = characterInfoArray[i].vertex_TL.uv4;
            //m_textInfo.meshInfo[0].uvs4[2 + index_X4] = characterInfoArray[i].vertex_TR.uv4;
            //m_textInfo.meshInfo[0].uvs4[3 + index_X4] = characterInfoArray[i].vertex_BR.uv4;


            // setup Vertex Colors
            m_textInfo.meshInfo[materialIndex].colors32[0 + index_X4] = characterInfoArray[i].vertex_BL.color;
            m_textInfo.meshInfo[materialIndex].colors32[1 + index_X4] = characterInfoArray[i].vertex_TL.color;
            m_textInfo.meshInfo[materialIndex].colors32[2 + index_X4] = characterInfoArray[i].vertex_TR.color;
            m_textInfo.meshInfo[materialIndex].colors32[3 + index_X4] = characterInfoArray[i].vertex_BR.color;

            m_textInfo.meshInfo[materialIndex].vertexCount = index_X4 + 4;
        }


        /// <summary>
        /// Method to add the underline geometry.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="maxScale"></param>
        /// <param name="underlineColor"></param>
        protected virtual void DrawUnderlineMesh(Vector3 start, Vector3 end, ref int index, float startScale, float endScale, float maxScale, Color32 underlineColor)
        {
            if (m_cached_Underline_GlyphInfo == null)
            {
                if (!TMP_Settings.warningsDisabled) Debug.LogWarning("Unable to add underline since the Font Asset doesn't contain the underline character.", this);
                return;
            }

            int verticesCount = index + 12;
            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (verticesCount > m_textInfo.meshInfo[0].vertices.Length)
            {
                // Resize Mesh Buffers
                m_textInfo.meshInfo[0].ResizeMeshInfo(verticesCount / 4);
            }

            // Adjust the position of the underline based on the lowest character. This matters for subscript character.
            start.y = Mathf.Min(start.y, end.y);
            end.y = Mathf.Min(start.y, end.y);

            float segmentWidth = m_cached_Underline_GlyphInfo.width / 2 * maxScale;

            if (end.x - start.x < m_cached_Underline_GlyphInfo.width * maxScale)
            {
                segmentWidth = (end.x - start.x) / 2f;
            }

            float startPadding = m_padding * startScale / maxScale;
            float endPadding = m_padding * endScale / maxScale;

            float underlineThickness = m_cached_Underline_GlyphInfo.height;

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            #region UNDERLINE VERTICES
            Vector3[] vertices = m_textInfo.meshInfo[0].vertices;

            // Front Part of the Underline
            vertices[index + 0] = start + new Vector3(0, 0 - (underlineThickness + m_padding) * maxScale, 0); // BL
            vertices[index + 1] = start + new Vector3(0, m_padding * maxScale, 0); // TL
            vertices[index + 2] = vertices[index + 1] + new Vector3(segmentWidth, 0, 0); // TR
            vertices[index + 3] = vertices[index + 0] + new Vector3(segmentWidth, 0, 0); // BR

            // Middle Part of the Underline
            vertices[index + 4] = vertices[index + 3]; // BL
            vertices[index + 5] = vertices[index + 2]; // TL
            vertices[index + 6] = end + new Vector3(-segmentWidth, m_padding * maxScale, 0);  // TR
            vertices[index + 7] = end + new Vector3(-segmentWidth, -(underlineThickness + m_padding) * maxScale, 0); // BR

            // End Part of the Underline
            vertices[index + 8] = vertices[index + 7]; // BL
            vertices[index + 9] = vertices[index + 6]; // TL
            vertices[index + 10] = end + new Vector3(0, m_padding * maxScale, 0); // TR
            vertices[index + 11] = end + new Vector3(0, -(underlineThickness + m_padding) * maxScale, 0); // BR
            #endregion

            // UNDERLINE UV0
            #region HANDLE UV0
            Vector2[] uvs0 = m_textInfo.meshInfo[0].uvs0;

            // Calculate UV required to setup the 3 Quads for the Underline.
            Vector2 uv0 = new Vector2((m_cached_Underline_GlyphInfo.x - startPadding) / m_fontAsset.fontInfo.AtlasWidth, 1 - (m_cached_Underline_GlyphInfo.y + m_padding + m_cached_Underline_GlyphInfo.height) / m_fontAsset.fontInfo.AtlasHeight);  // bottom left
            Vector2 uv1 = new Vector2(uv0.x, 1 - (m_cached_Underline_GlyphInfo.y - m_padding) / m_fontAsset.fontInfo.AtlasHeight);  // top left
            Vector2 uv2 = new Vector2((m_cached_Underline_GlyphInfo.x - startPadding + m_cached_Underline_GlyphInfo.width / 2) / m_fontAsset.fontInfo.AtlasWidth, uv1.y); // Mid Top Left
            Vector2 uv3 = new Vector2(uv2.x, uv0.y); // Mid Bottom Left
            Vector2 uv4 = new Vector2((m_cached_Underline_GlyphInfo.x + endPadding + m_cached_Underline_GlyphInfo.width / 2) / m_fontAsset.fontInfo.AtlasWidth, uv1.y); // Mid Top Right
            Vector2 uv5 = new Vector2(uv4.x, uv0.y); // Mid Bottom right
            Vector2 uv6 = new Vector2((m_cached_Underline_GlyphInfo.x + endPadding + m_cached_Underline_GlyphInfo.width) / m_fontAsset.fontInfo.AtlasWidth, uv1.y); // End Part - Bottom Right
            Vector2 uv7 = new Vector2(uv6.x, uv0.y); // End Part - Top Right

            // Left Part of the Underline
            uvs0[0 + index] = uv0; // BL
            uvs0[1 + index] = uv1; // TL
            uvs0[2 + index] = uv2; // TR
            uvs0[3 + index] = uv3; // BR

            // Middle Part of the Underline
            uvs0[4 + index] = new Vector2(uv2.x - uv2.x * 0.001f, uv0.y);
            uvs0[5 + index] = new Vector2(uv2.x - uv2.x * 0.001f, uv1.y);
            uvs0[6 + index] = new Vector2(uv2.x + uv2.x * 0.001f, uv1.y);
            uvs0[7 + index] = new Vector2(uv2.x + uv2.x * 0.001f, uv0.y);

            // Right Part of the Underline
            uvs0[8 + index] = uv5;
            uvs0[9 + index] = uv4;
            uvs0[10 + index] = uv6;
            uvs0[11 + index] = uv7;
            #endregion

            // UNDERLINE UV2
            #region HANDLE UV2 - SDF SCALE
            // UV1 contains Face / Border UV layout.
            float min_UvX = 0;
            float max_UvX = (vertices[index + 2].x - start.x) / (end.x - start.x);

            //Calculate the xScale or how much the UV's are getting stretched on the X axis for the middle section of the underline.
            float xScale = maxScale * m_rectTransform.lossyScale.y == 0 ? 1 : m_rectTransform.lossyScale.y;
            float xScale2 = xScale;

            Vector2[] uvs2 = m_textInfo.meshInfo[0].uvs2;

            uvs2[0 + index] = PackUV(0, 0, xScale);
            uvs2[1 + index] = PackUV(0, 1, xScale);
            uvs2[2 + index] = PackUV(max_UvX, 1, xScale);
            uvs2[3 + index] = PackUV(max_UvX, 0, xScale);

            min_UvX = (vertices[index + 4].x - start.x) / (end.x - start.x);
            max_UvX = (vertices[index + 6].x - start.x) / (end.x - start.x);

            uvs2[4 + index] = PackUV(min_UvX, 0, xScale2);
            uvs2[5 + index] = PackUV(min_UvX, 1, xScale2);
            uvs2[6 + index] = PackUV(max_UvX, 1, xScale2);
            uvs2[7 + index] = PackUV(max_UvX, 0, xScale2);

            min_UvX = (vertices[index + 8].x - start.x) / (end.x - start.x);
            max_UvX = (vertices[index + 6].x - start.x) / (end.x - start.x);

            uvs2[8 + index] = PackUV(min_UvX, 0, xScale);
            uvs2[9 + index] = PackUV(min_UvX, 1, xScale);
            uvs2[10 + index] = PackUV(1, 1, xScale);
            uvs2[11 + index] = PackUV(1, 0, xScale);
            #endregion

            // UNDERLINE VERTEX COLORS
            #region
            //underlineColor.a /= 2; // Alpha value needs to be adjusted since bold is encoded in it.
            Color32[] colors32 = m_textInfo.meshInfo[0].colors32;

            colors32[0 + index] = underlineColor;
            colors32[1 + index] = underlineColor;
            colors32[2 + index] = underlineColor;
            colors32[3 + index] = underlineColor;

            colors32[4 + index] = underlineColor;
            colors32[5 + index] = underlineColor;
            colors32[6 + index] = underlineColor;
            colors32[7 + index] = underlineColor;

            colors32[8 + index] = underlineColor;
            colors32[9 + index] = underlineColor;
            colors32[10 + index] = underlineColor;
            colors32[11 + index] = underlineColor;
            #endregion

            index += 12;
        }


        /// <summary>
        /// Method used to find and cache references to the Underline and Ellipsis characters.
        /// </summary>
        /// <param name=""></param>
        protected void GetSpecialCharacters(TMP_FontAsset fontAsset)
        {
            // Check & Assign Underline Character for use with the Underline tag.
            if (!fontAsset.characterDictionary.TryGetValue(95, out m_cached_Underline_GlyphInfo)) //95
            {
                // Check fallback fonts
                    // TODO

                if (!TMP_Settings.warningsDisabled)
                    Debug.LogWarning("Underscore character wasn't found in the current Font Asset [" + fontAsset.name + "]. No characters assigned for Underline.\nNote: Warnings can be disabled in the TMP Settings file.", this);
            }

            // Check & Assign Underline Character for use with the Underline tag.
            if (!fontAsset.characterDictionary.TryGetValue(8230, out m_cached_Ellipsis_GlyphInfo)) //95
            {
                // Check fallback fonts
                // TODO

                if (!TMP_Settings.warningsDisabled)
                    Debug.LogWarning("Ellipsis character wasn't found in the current Font Asset [" + fontAsset.name + "]. No characters assigned for Ellipsis.\nNote: Warnings can be disabled in the TMP Settings file.", this);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected int GetMaterialReferenceForFontWeight()
        {
            //bool isItalic = (m_style & FontStyles.Italic) == FontStyles.Italic || (m_fontStyle & FontStyles.Italic) == FontStyles.Italic;

            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentFontAsset.fontWeights[0].italicTypeface.material, m_currentFontAsset.fontWeights[0].italicTypeface, m_materialReferences, m_materialReferenceIndexLookup);

            return 0;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected TMP_FontAsset GetAlternativeFontAsset()
        {
            bool isItalic = (m_style & FontStyles.Italic) == FontStyles.Italic || (m_fontStyle & FontStyles.Italic) == FontStyles.Italic;

            TMP_FontAsset fontAsset = null;

            int weightIndex = m_fontWeightInternal / 100;

            if (isItalic)
                fontAsset = m_currentFontAsset.fontWeights[weightIndex].italicTypeface;
            else
                fontAsset = m_currentFontAsset.fontWeights[weightIndex].regularTypeface;

            if (fontAsset == null) return m_currentFontAsset;

            m_currentFontAsset = fontAsset;

            return m_currentFontAsset;
        }



        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        //protected Vector2 PackUV(float x, float y, float scale)
        //{
        //    Vector2 output;

        //    output.x = Mathf.Floor(x * 4095);
        //    output.y = Mathf.Floor(y * 4095);

        //    output.x = (output.x * 4096) + output.y;
        //    output.y = scale;

        //    return output;
        //}

        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        protected Vector2 PackUV(float x, float y, float scale)
        {
            Vector2 output;

            output.x = Mathf.Floor(x * 511);
            output.y = Mathf.Floor(y * 511);

            output.x = (output.x * 4096) + output.y;
            output.y = scale;

            return output;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected float PackUV(float x, float y)
        {
            double x0 = Math.Floor(x * 511);
            double y0 = Math.Floor(y * 511);

            return (float)((x0 * 4096) + y0);
        }


        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        //protected Vector2 PackUV(float x, float y, float scale)
        //{
        //    Vector2 output;

        //    output.x = Mathf.Floor(x * 4095);
        //    output.y = Mathf.Floor(y * 4095);

        //    return new Vector2((output.x * 4096) + output.y, scale);
        //}


        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        //protected float PackUV(float x, float y)
        //{
        //    x = (x % 5) / 5;
        //    y = (y % 5) / 5;

        //    return Mathf.Round(x * 4096) + y;
        //}


        /// <summary>
        /// Method to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        protected int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }


        /// <summary>
        /// Convert UTF-16 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        protected int GetUTF16(int i)
        {
            int unicode = HexToInt(m_text[i]) * 4096;
            unicode += HexToInt(m_text[i + 1]) * 256;
            unicode += HexToInt(m_text[i + 2]) * 16;
            unicode += HexToInt(m_text[i + 3]);
            return unicode;
        }


        /// <summary>
        /// Convert UTF-32 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        protected int GetUTF32(int i)
        {
            int unicode = 0;
            unicode += HexToInt(m_text[i]) * 268435456;
            unicode += HexToInt(m_text[i + 1]) * 16777216;
            unicode += HexToInt(m_text[i + 2]) * 1048576;
            unicode += HexToInt(m_text[i + 3]) * 65536;
            unicode += HexToInt(m_text[i + 4]) * 4096;
            unicode += HexToInt(m_text[i + 5]) * 256;
            unicode += HexToInt(m_text[i + 6]) * 16;
            unicode += HexToInt(m_text[i + 7]);
            return unicode;
        }


        /// <summary>
        /// Method to convert Hex color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="tagCount"></param>
        /// <returns></returns>
        protected Color32 HexCharsToColor(char[] hexChars, int tagCount)
        {
            if (tagCount == 7)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 9)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));
                byte a = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));

                return new Color32(r, g, b, a);
            }
            else if (tagCount == 13)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));

                return new Color32(r, g, b, 255);
            }
            else if (tagCount == 15)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));
                byte a = (byte)(HexToInt(hexChars[13]) * 16 + HexToInt(hexChars[14]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Method to convert Hex Color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        protected Color32 HexCharsToColor(char[] hexChars, int startIndex, int length)
        {
            if (length == 7)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));

                return new Color32(r, g, b, 255);
            }
            else if (length == 9)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));
                byte a = (byte)(HexToInt(hexChars[startIndex + 7]) * 16 + HexToInt(hexChars[startIndex + 8]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }


        /// <summary>
        /// Extracts a float value from char[] assuming we know the position of the start, end and decimal point.
        /// </summary>
        /// <param name="chars"></param> The Char[] containing the numerical sequence.
        /// <param name="startIndex"></param> The index of the start of the numerical sequence.
        /// <param name="endIndex"></param> The index of the last number in the numerical sequence.
        /// <param name="decimalPointIndex"></param> The index of the decimal point if any.
        /// <returns></returns>
        protected float ConvertToFloat(char[] chars, int startIndex, int length, int decimalPointIndex)
        {
            if (startIndex == 0) return -9999;

            int endIndex = startIndex + length - 1;
            float v = 0;
            float sign = 1;
            decimalPointIndex = decimalPointIndex > 0 ? decimalPointIndex : endIndex + 1; // Check in case we don't have any decimal point

            // Check if negative value
            if (chars[startIndex] == 45) // '-'
            {
                startIndex += 1;
                sign = -1;
            }

            if (chars[startIndex] == 43 || chars[startIndex] == 37) startIndex += 1; // '+'


            for (int i = startIndex; i < endIndex + 1; i++)
            {
                if (!char.IsDigit(chars[i]) && chars[i] != 46) return -9999;

                switch (decimalPointIndex - i)
                {
                    case 4:
                        v += (chars[i] - 48) * 1000;
                        break;
                    case 3:
                        v += (chars[i] - 48) * 100;
                        break;
                    case 2:
                        v += (chars[i] - 48) * 10;
                        break;
                    case 1:
                        v += (chars[i] - 48);
                        break;
                    case -1:
                        v += (chars[i] - 48) * 0.1f;
                        break;
                    case -2:
                        v += (chars[i] - 48) * 0.01f;
                        break;
                    case -3:
                        v += (chars[i] - 48) * 0.001f;
                        break;
                }
            }
            return v * sign;
        }


        /// <summary>
        /// Function to identify and validate the rich tag. Returns the position of the > if the tag was valid.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        protected bool ValidateHtmlTag(int[] chars, int startIndex, out int endIndex)
        {
            int tagCharCount = 0;
            byte attributeFlag = 0;

            TagUnits tagUnits = TagUnits.Pixels;
            TagType tagType = TagType.None;

            int attributeIndex = 0;
            m_xmlAttribute[attributeIndex].nameHashCode = 0;
            m_xmlAttribute[attributeIndex].valueType = TagType.None;
            m_xmlAttribute[attributeIndex].valueHashCode = 0;
            m_xmlAttribute[attributeIndex].valueStartIndex = 0;
            m_xmlAttribute[attributeIndex].valueLength = 0;
            m_xmlAttribute[attributeIndex].valueDecimalIndex = 0;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;

            for (int i = startIndex; i < chars.Length && chars[i] != 0 && tagCharCount < m_htmlTag.Length && chars[i] != 60; i++)
            {
                if (chars[i] == 62) // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    m_htmlTag[tagCharCount] = (char)0;
                    break;
                }

                m_htmlTag[tagCharCount] = (char)chars[i];
                tagCharCount += 1;


                if (attributeFlag == 1)
                {
                    if (m_xmlAttribute[attributeIndex].valueStartIndex == 0)
                    {
                        // Check for attribute type
                        if (chars[i] == 43 || chars[i] == 45 || char.IsDigit((char)chars[i]))
                        {
                            tagType = TagType.NumericalValue;
                            m_xmlAttribute[attributeIndex].valueType = TagType.NumericalValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (chars[i] == 35)
                        {
                            tagType = TagType.ColorValue;
                            m_xmlAttribute[attributeIndex].valueType = TagType.ColorValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (chars[i] != 34)
                        {
                            tagType = TagType.StringValue;
                            m_xmlAttribute[attributeIndex].valueType = TagType.StringValue;
                            m_xmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_xmlAttribute[attributeIndex].valueHashCode = (m_xmlAttribute[attributeIndex].valueHashCode << 5) + m_xmlAttribute[attributeIndex].valueHashCode ^ chars[i];
                            m_xmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (tagType == TagType.NumericalValue)
                        {
                            if (chars[i] == 46) // '.' Decimal Point Index
                                m_xmlAttribute[attributeIndex].valueDecimalIndex = tagCharCount - 1;

                            // Check for termination of numerical value.
                            if (chars[i] == 112 || chars[i] == 101 || chars[i] == 37 || chars[i] == 32)
                            {
                                attributeFlag = 2;
                                tagType = TagType.None;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagType.None;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                                m_xmlAttribute[attributeIndex].valueDecimalIndex = 0;

                                if (chars[i] == 101)
                                    tagUnits = TagUnits.FontUnits;
                                else if (chars[i] == 37)
                                    tagUnits = TagUnits.Percentage;
                            }
                            else if (attributeFlag != 2)
                            {
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (tagType == TagType.ColorValue)
                        {
                            if (chars[i] != 32)
                            {
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagType = TagType.None;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagType.None;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                                m_xmlAttribute[attributeIndex].valueDecimalIndex = 0;
                            }
                        }
                        else if (tagType == TagType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            if (chars[i] != 34)
                            {
                                m_xmlAttribute[attributeIndex].valueHashCode = (m_xmlAttribute[attributeIndex].valueHashCode << 5) + m_xmlAttribute[attributeIndex].valueHashCode ^ chars[i];
                                m_xmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                //m_xmlAttribute[attributeIndex].valueHashCode = -1;
                                attributeFlag = 2;
                                tagType = TagType.None;
                                attributeIndex += 1;
                                m_xmlAttribute[attributeIndex].nameHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueType = TagType.None;
                                m_xmlAttribute[attributeIndex].valueHashCode = 0;
                                m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_xmlAttribute[attributeIndex].valueLength = 0;
                                m_xmlAttribute[attributeIndex].valueDecimalIndex = 0;
                            }
                        }
                    }
                }


                if (chars[i] == 61) // '=' 
                    attributeFlag = 1;

                // Compute HashCode for the name of the attribute
                if (attributeFlag == 0 && chars[i] == 32)
                {
                    if (isTagSet) return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    tagType = TagType.None;
                    attributeIndex += 1;
                    m_xmlAttribute[attributeIndex].nameHashCode = 0;
                    m_xmlAttribute[attributeIndex].valueType = TagType.None;
                    m_xmlAttribute[attributeIndex].valueHashCode = 0;
                    m_xmlAttribute[attributeIndex].valueStartIndex = 0;
                    m_xmlAttribute[attributeIndex].valueLength = 0;
                    m_xmlAttribute[attributeIndex].valueDecimalIndex = 0;
                }

                if (attributeFlag == 0)
                    m_xmlAttribute[attributeIndex].nameHashCode = (m_xmlAttribute[attributeIndex].nameHashCode << 3) - m_xmlAttribute[attributeIndex].nameHashCode + chars[i];

                if (attributeFlag == 2 && chars[i] == 32)
                    attributeFlag = 0;

            }

            if (!isValidHtmlTag)
            {
                return false;
            }

            //Debug.Log("Tag is [" + m_htmlTag.ArrayToString() + "].  Tag HashCode: " + m_xmlAttribute[0].nameHashCode + "  Tag Value HashCode: " + m_xmlAttribute[0].valueHashCode + "  Attribute 1 HashCode: " + m_xmlAttribute[1].nameHashCode + " Value HashCode: " + m_xmlAttribute[1].valueHashCode);
            //for (int i = 0; i < attributeIndex + 1; i++)
            //    Debug.Log("Tag [" + i + "] with HashCode: " + m_xmlAttribute[i].nameHashCode + " has value of [" + new string(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength) + "] Numerical Value: " + ConvertToFloat(m_htmlTag, m_xmlAttribute[i].valueStartIndex, m_xmlAttribute[i].valueLength, m_xmlAttribute[i].valueDecimalIndex));


            // Special handling of the NoParsing tag
            if (tag_NoParsing && m_xmlAttribute[0].nameHashCode != 53822163)
                return false;
            else if (m_xmlAttribute[0].nameHashCode == 53822163)
            {
                tag_NoParsing = false;
                return true;
            }

            // Color <#FF00FF>
            if (m_htmlTag[0] == 35 && tagCharCount == 7) // if Tag begins with # and contains 7 characters. 
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            // Color <#FF00FF00> with alpha
            else if (m_htmlTag[0] == 35 && tagCharCount == 9) // if Tag begins with # and contains 9 characters. 
            {
                m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                m_colorStack.Add(m_htmlColor);
                return true;
            }
            else
            {
                float value = 0;

                switch (m_xmlAttribute[0].nameHashCode)
                {
                    case 98: // <b>
                        m_style |= FontStyles.Bold;
                        m_fontWeightInternal = 700;
                        m_fontWeightStack.Add(700);
                        return true;
                    case 427: // </b>
                        if ((m_fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            m_style &= ~FontStyles.Bold;
                            m_fontWeightInternal = m_fontWeightStack.Remove();
                        }
                        return true;
                    case 105: // <i>
                        m_style |= FontStyles.Italic;
                        return true;
                    case 434: // </i>
                        m_style &= ~FontStyles.Italic;
                        return true;
                    case 115: // <s>
                        m_style |= FontStyles.Strikethrough;
                        return true;
                    case 444: // </s>
                        if ((m_fontStyle & FontStyles.Strikethrough) != FontStyles.Strikethrough)
                            m_style &= ~FontStyles.Strikethrough;
                        return true;
                    case 117: // <u>
                        m_style |= FontStyles.Underline;
                        return true;
                    case 446: // </u>
                        if ((m_fontStyle & FontStyles.Underline) != FontStyles.Underline)
                            m_style &= ~FontStyles.Underline;
                        return true;

                    case 6552: // <sub>
                        m_fontScaleMultiplier = m_currentFontAsset.fontInfo.SubSize > 0 ? m_currentFontAsset.fontInfo.SubSize : 1;
                        m_baselineOffset = m_currentFontAsset.fontInfo.SubscriptOffset * m_fontScale * m_fontScaleMultiplier;
                        m_style |= FontStyles.Subscript;
                        return true;
                    case 22673: // </sub>
                        if ((m_style & FontStyles.Subscript) == FontStyles.Subscript)
                        {
                            // Check to make sure we are not also using Superscript
                            if ((m_style & FontStyles.Superscript) == FontStyles.Superscript)
                            {
                                m_fontScaleMultiplier = m_currentFontAsset.fontInfo.SubSize > 0 ? m_currentFontAsset.fontInfo.SubSize : 1;
                                m_baselineOffset = m_currentFontAsset.fontInfo.SuperscriptOffset * m_fontScale * m_fontScaleMultiplier;
                            }
                            else
                            {
                                m_baselineOffset = 0;
                                m_fontScaleMultiplier = 1;
                            }

                            m_style &= ~FontStyles.Subscript;
                        }
                        return true;
                    case 6566: // <sup>
                        m_fontScaleMultiplier = m_currentFontAsset.fontInfo.SubSize > 0 ? m_currentFontAsset.fontInfo.SubSize : 1;
                        m_baselineOffset = m_currentFontAsset.fontInfo.SuperscriptOffset * m_fontScale * m_fontScaleMultiplier;
                        m_style |= FontStyles.Superscript;
                        return true;
                    case 22687: // </sup>
                        if ((m_style & FontStyles.Superscript) == FontStyles.Superscript)
                        {
                            // Check to make sure we are not also using Superscript
                            if ((m_style & FontStyles.Subscript) == FontStyles.Subscript)
                            {
                                m_fontScaleMultiplier = m_currentFontAsset.fontInfo.SubSize > 0 ? m_currentFontAsset.fontInfo.SubSize : 1;
                                m_baselineOffset = m_currentFontAsset.fontInfo.SubscriptOffset * m_fontScale * m_fontScaleMultiplier;
                            }
                            else
                            {
                                m_baselineOffset = 0;
                                m_fontScaleMultiplier = 1;
                            }

                            m_style &= ~FontStyles.Superscript;
                        }
                        return true;
                    case -330774850: // <font-weight>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        if ((m_fontStyle & FontStyles.Bold) == FontStyles.Bold)
                        {
                            // Nothing happens since Bold is forced on the text.
                            //m_fontWeight = 700;
                            return true;
                        }


                        // Remove bold style
                        m_style &= ~FontStyles.Bold;

                        switch ((int)value)
                        {
                            case 100:
                                m_fontWeightInternal = 100;
                                break;
                            case 200:
                                m_fontWeightInternal = 200;
                                break;
                            case 300:
                                m_fontWeightInternal = 300;
                                break;
                            case 400:
                                m_fontWeightInternal = 400;

                                break;
                            case 500:
                                m_fontWeightInternal = 500;
                                break;
                            case 600:
                                m_fontWeightInternal = 600;
                                break;
                            case 700:
                                m_fontWeightInternal = 700;
                                m_style |= FontStyles.Bold;
                                break;
                            case 800:
                                m_fontWeightInternal = 800;
                                break;
                            case 900:
                                m_fontWeightInternal = 900;
                                break;
                        }

                        m_fontWeightStack.Add(m_fontWeightInternal);

                        return true;
                    case -1885698441: // </font-weight>
                        m_fontWeightInternal = m_fontWeightStack.Remove();
                        return true;
                    case 6380: // <pos=000.00px> <pos=0em> <pos=50%>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                m_xAdvance = value;
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnits.FontUnits:
                                m_xAdvance = value * m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnits.Percentage:
                                m_xAdvance = m_marginWidth * value / 100;
                                //m_isIgnoringAlignment = true;
                                return true;
                        }
                        return false;
                    case 22501: // </pos>
                        m_isIgnoringAlignment = false;
                        return true;
                    case 16034505: // <voffset>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                m_baselineOffset = value;
                                return true;
                            case TagUnits.FontUnits:
                                m_baselineOffset = value * m_fontScale * m_fontAsset.fontInfo.Ascender;
                                return true;
                            case TagUnits.Percentage:
                                //m_baselineOffset = m_marginHeight * val / 100;
                                return false;
                        }
                        return false;
                    case 54741026: // </voffset>
                        m_baselineOffset = 0;
                        return true;
                    case 43991: // <page>
                        // This tag only works when Overflow - Page mode is used.
                        if (m_overflowMode == TextOverflowModes.Page)
                        {
                            m_xAdvance = 0 + tag_LineIndent + tag_Indent;
                            //m_textInfo.lineInfo[m_lineNumber].marginLeft = m_xAdvance;
                            m_lineOffset = 0;
                            m_pageNumber += 1;
                            m_isNewPage = true;
                        }
                        return true;

                    case 43969: // <nobr>
                        m_isNonBreakingSpace = true;
                        return true;
                    case 156816: // </nobr>
                        m_isNonBreakingSpace = false;
                        return true;
                    case 45545: // <size=>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                if (m_htmlTag[5] == 43) // <size=+00>
                                {
                                    m_currentFontSize = m_fontSize + value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                                    return true;
                                }
                                else if (m_htmlTag[5] == 45) // <size=-00>
                                {
                                    m_currentFontSize = m_fontSize + value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                                    return true;
                                }
                                else // <size=00.0>
                                {
                                    m_currentFontSize = value;
                                    m_sizeStack.Add(m_currentFontSize);
                                    m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                                    return true;
                                }
                            case TagUnits.FontUnits:
                                m_currentFontSize = m_fontSize * value;
                                m_sizeStack.Add(m_currentFontSize);
                                m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                                return true;
                            case TagUnits.Percentage:
                                m_currentFontSize = m_fontSize * value / 100;
                                m_sizeStack.Add(m_currentFontSize);
                                m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                                return true;
                        }
                        return false;
                    case 158392: // </size>
                        m_currentFontSize = m_sizeStack.Remove();
                        m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                        return true;
                    case 41311: // <font=xx>
                        //Debug.Log("Font name: \"" + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength) + "\"   HashCode: " + m_xmlAttribute[0].valueHashCode + "   Material Name: \"" + new string(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength) + "\"   Hashcode: " + m_xmlAttribute[1].valueHashCode);

                        int fontHashCode = m_xmlAttribute[0].valueHashCode;
                        int materialAttributeHashCode = m_xmlAttribute[1].nameHashCode;
                        int materialHashCode = m_xmlAttribute[1].valueHashCode;

                        // Special handling for <font=default> or <font=Default>
                        if (fontHashCode == 764638571 || fontHashCode == 523367755)
                        {
                            m_currentFontAsset = m_materialReferences[0].fontAsset;
                            m_currentMaterial = m_materialReferences[0].material;
                            m_currentMaterialIndex = 0;
                            //Debug.Log("<font=Default> assigning Font Asset [" + m_currentFontAsset.name + "] with Material [" + m_currentMaterial.name + "].");

                            m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));

                            m_materialReferenceStack.Add(m_materialReferences[0]);

                            return true;
                        }

                        TMP_FontAsset tempFont;
                        Material tempMaterial;

                        // HANDLE NEW FONT ASSET
                        if (MaterialReferenceManager.TryGetFontAsset(fontHashCode, out tempFont))
                        {
                            //if (tempFont != m_currentFontAsset)
                            //{
                            //    //Debug.Log("Assigning Font Asset: " + tempFont.name);
                            //    m_currentFontAsset = tempFont;
                            //    m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));
                            //}
                        }
                        else
                        {
                            // Load Font Asset
                            tempFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/" + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));

                            if (tempFont == null)
                                return false;

                            // Add new reference to the font asset as well as default material to the MaterialReferenceManager
                            MaterialReferenceManager.AddFontAsset(tempFont);
                        }


                        // HANDLE NEW MATERIAL
                        if (materialAttributeHashCode == 0 && materialHashCode == 0)
                        {
                            // No material specified then use default font asset material.
                            m_currentMaterial = tempFont.material;

                            m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, m_materialReferences, m_materialReferenceIndexLookup);

                            m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                        }
                        else if (materialAttributeHashCode == 103415287) // using material attribute
                        {
                            if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                            {
                                m_currentMaterial = tempMaterial;

                                m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, m_materialReferences, m_materialReferenceIndexLookup);

                                m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                            }
                            else
                            {
                                // Load new material
                                tempMaterial = Resources.Load<Material>("Fonts & Materials/" + new string(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength));

                                if (tempMaterial == null)
                                    return false;

                                // Add new reference to this material in the MaterialReferenceManager
                                MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                                m_currentMaterial = tempMaterial;

                                m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentMaterial, tempFont, m_materialReferences, m_materialReferenceIndexLookup);

                                m_materialReferenceStack.Add(m_materialReferences[m_currentMaterialIndex]);
                            }
                        }
                        else
                            return false;

                        m_currentFontAsset = tempFont;
                        m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));

                        return true;
                    case 154158: // </font>
                        MaterialReference materialReference = m_materialReferenceStack.Remove();

                        m_currentFontAsset = materialReference.fontAsset;
                        m_currentMaterial = materialReference.material;
                        m_currentMaterialIndex = materialReference.index;

                        m_fontScale = (m_currentFontSize / m_currentFontAsset.fontInfo.PointSize * m_currentFontAsset.fontInfo.Scale * (m_isOrthographic ? 1 : 0.1f));

                        return true;
                    case 320078: // <space=000.00>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                m_xAdvance += value;
                                return true;
                            case TagUnits.FontUnits:
                                m_xAdvance += value * m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                return true;
                            case TagUnits.Percentage:
                                // Not applicable
                                return false;
                        }
                        return false;
                    case 276254: // <alpha=#FF>
                        if (m_xmlAttribute[0].valueLength != 3) return false;

                        m_htmlColor.a = (byte)(HexToInt(m_htmlTag[7]) * 16 + HexToInt(m_htmlTag[8]));
                        return true;

                    case 1750458: // <a name=" ">
                        return false;
                    case 426: // </a>
                        return true;
                    case 43066: // <link="name">
                        if (m_isParsingText)
                        {
                            int size = m_textInfo.linkInfo.Length;

                            if (m_textInfo.linkCount + 1 > size)
                                TMP_TextInfo.Resize(ref m_textInfo.linkInfo, size + 1);

                            int index = m_textInfo.linkCount;
                            m_textInfo.linkInfo[index].textComponent = this;
                            m_textInfo.linkInfo[index].hashCode = m_xmlAttribute[0].valueHashCode;
                            m_textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_characterCount;

                            m_textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_xmlAttribute[0].valueStartIndex;
                            m_textInfo.linkInfo[index].linkIdLength = m_xmlAttribute[0].valueLength;
                            m_textInfo.linkInfo[index].SetLinkID(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength);
                        }
                        return true;
                    case 155913: // </link>
                        if (m_isParsingText)
                        {
                            m_textInfo.linkInfo[m_textInfo.linkCount].linkTextLength = m_characterCount - m_textInfo.linkInfo[m_textInfo.linkCount].linkTextfirstCharacterIndex;

                            m_textInfo.linkCount += 1;

                        }
                        return true;
                    case 275917: // <align=>
                        switch (m_xmlAttribute[0].valueHashCode)
                        {
                            case 3774683: // <align=left>
                                m_lineJustification = TextAlignmentOptions.Left;
                                return true;
                            case 136703040: // <align=right>
                                m_lineJustification = TextAlignmentOptions.Right;
                                return true;
                            case -458210101: // <align=center>
                                m_lineJustification = TextAlignmentOptions.Center;
                                return true;
                            case -523808257: // <align=justified>
                                m_lineJustification = TextAlignmentOptions.Justified;
                                return true;
                        }
                        return false;
                    case 1065846: // </align>
                        m_lineJustification = m_textAlignment;
                        return true;
                    case 327550: // <width=xx>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                m_width = value;
                                break;
                            case TagUnits.FontUnits:
                                return false;
                            //break;
                            case TagUnits.Percentage:
                                m_width = m_marginWidth * value / 100;
                                break;
                        }
                        return true;
                    case 1117479: // </width>
                        m_width = -1;
                        return true;
                    case 322689: // <style="name">
                        TMP_Style style = TMP_StyleSheet.GetStyle(m_xmlAttribute[0].valueHashCode);

                        if (style == null) return false;

                        m_styleStack.Add(style.hashCode);

                        //// Parse Style Macro
                        for (int i = 0; i < style.styleOpeningTagArray.Length; i++)
                        {
                            if (style.styleOpeningTagArray[i] == 60)
                                ValidateHtmlTag(style.styleOpeningTagArray, i + 1, out i);
                        }
                        return true;
                    case 1112618: // </style>
                        style = TMP_StyleSheet.GetStyle(m_xmlAttribute[0].valueHashCode);

                        if (style == null)
                        {
                            // Get style from the Style Stack
                            int styleHashCode = m_styleStack.Remove();
                            style = TMP_StyleSheet.GetStyle(styleHashCode);
                        }

                        if (style == null) return false;
                        //// Parse Style Macro
                        for (int i = 0; i < style.styleClosingTagArray.Length; i++)
                        {
                            if (style.styleClosingTagArray[i] == 60)
                                ValidateHtmlTag(style.styleClosingTagArray, i + 1, out i);
                        }
                        return true;
                    case 281955: // <color=#FF00FF> or <color=#FF00FF00>
                        // <color=#FF00FF> 3 Hex pairs
                        if (m_htmlTag[6] == 35 && tagCharCount == 13)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }
                        // <color=#FF00FF00> 4 Hex pairs
                        else if (m_htmlTag[6] == 35 && tagCharCount == 15)
                        {
                            m_htmlColor = HexCharsToColor(m_htmlTag, tagCharCount);
                            m_colorStack.Add(m_htmlColor);
                            return true;
                        }

                        // <color=name>
                        switch (m_xmlAttribute[0].valueHashCode)
                        {
                            case 125395: // <color=red>
                                m_htmlColor = Color.red;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 3573310: // <color=blue>
                                m_htmlColor = Color.blue;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 117905991: // <color=black>
                                m_htmlColor = Color.black;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 121463835: // <color=green>
                                m_htmlColor = Color.green;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 140357351: // <color=white>
                                m_htmlColor = Color.white;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 26556144: // <color=orange>
                                m_htmlColor = new Color32(255, 128, 0, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case -36881330: // <color=purple>
                                m_htmlColor = new Color32(160, 32, 240, 255);
                                m_colorStack.Add(m_htmlColor);
                                return true;
                            case 554054276: // <color=yellow>
                                m_htmlColor = Color.yellow;
                                m_colorStack.Add(m_htmlColor);
                                return true;
                        }
                        return false;
                    case 1983971: // <cspace=xx.x>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                m_cSpacing = value;
                                break;
                            case TagUnits.FontUnits:
                                m_cSpacing = value;
                                m_cSpacing *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                return false;
                        }
                        return true;
                    case 7513474: // </cspace>
                        m_cSpacing = 0;
                        return true;
                    case 2152041: // <mspace=xx.x>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                m_monoSpacing = value;
                                break;
                            case TagUnits.FontUnits:
                                m_monoSpacing = value;
                                m_monoSpacing *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                return false;
                        }
                        return true;
                    case 7681544: // </mspace>
                        m_monoSpacing = 0;
                        return true;
                    case 280416: // <class="name">
                        return false;
                    case 1071884: // </color>
                        m_htmlColor = m_colorStack.Remove();
                        return true;
                    case 2068980: // <indent=10px> <indent=10em> <indent=50%>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                tag_Indent = value;
                                break;
                            case TagUnits.FontUnits:
                                tag_Indent = value;
                                tag_Indent *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                tag_Indent = m_marginWidth * value / 100;
                                break;
                        }
                        m_indentStack.Add(tag_Indent);

                        m_xAdvance = tag_Indent;
                        return true;
                    case 7598483: // </indent>
                        tag_Indent = m_indentStack.Remove();
                        //m_xAdvance = tag_Indent;
                        return true;
                    case 1109386397: // <line-indent>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                tag_LineIndent = value;
                                break;
                            case TagUnits.FontUnits:
                                tag_LineIndent = value;
                                tag_LineIndent *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                tag_LineIndent = m_marginWidth * value / 100;
                                break;
                        }

                        m_xAdvance += tag_LineIndent;
                        return true;
                    case -445537194: // </line-indent>
                        tag_LineIndent = 0;
                        return true;
                    case 2246877: // <sprite=x>
                        int spriteAssetHashCode = m_xmlAttribute[0].valueHashCode;
                        TMP_SpriteAsset tempSpriteAsset;

                        // CHECK TAG FORMAT
                        if (m_xmlAttribute[0].valueType == TagType.None || m_xmlAttribute[0].valueType == TagType.NumericalValue)
                        {
                            // No Sprite Asset specified
                            if (m_defaultSpriteAsset == null)
                            {
                                if (TMP_Settings.defaultSpriteAsset != null)
                                    m_defaultSpriteAsset = TMP_Settings.defaultSpriteAsset;
                                else
                                    m_defaultSpriteAsset = Resources.Load<TMP_SpriteAsset>("Sprite Assets/Default Sprite Asset");

                            }

                            m_currentSpriteAsset = m_defaultSpriteAsset;

                            // No valid sprite asset available
                            if (m_currentSpriteAsset == null)
                                return false;
                        }
                        else
                        {
                            // A Sprite Asset has been specified
                            if (MaterialReferenceManager.TryGetSpriteAsset(spriteAssetHashCode, out tempSpriteAsset))
                            {
                                m_currentSpriteAsset = tempSpriteAsset;
                            }
                            else
                            {
                                // Load Sprite Asset
                                if (tempSpriteAsset == null)
                                {
                                    tempSpriteAsset = Resources.Load<TMP_SpriteAsset>("Sprites/" + new string(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength));
                                }

                                if (tempSpriteAsset == null)
                                    return false;

                                //Debug.Log("Loading & assigning new Sprite Asset: " + tempSpriteAsset.name);
                                MaterialReferenceManager.AddSpriteAsset(spriteAssetHashCode, tempSpriteAsset);
                                m_currentSpriteAsset = tempSpriteAsset;
                            }
                        }


                        if (m_xmlAttribute[0].valueType == TagType.NumericalValue) // <sprite=index>
                        {
                            int index = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                            if (index == -9999) return false;

                            // Check to make sure sprite index is valid
                            if (index > m_currentSpriteAsset.spriteInfoList.Count - 1) return false;

                            m_spriteIndex = index;
                        }
                        else if (m_xmlAttribute[1].nameHashCode == 43347) // <sprite name="">
                        {
                            int index = m_currentSpriteAsset.GetSpriteIndex(m_xmlAttribute[1].valueHashCode);
                            if (index == -1) return false;

                            m_spriteIndex = index;
                        }
                        else if (m_xmlAttribute[1].nameHashCode == 295562) // <sprite index=xx>
                        {
                            int index = (int)ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength, m_xmlAttribute[1].valueDecimalIndex);
                            if (index == -9999) return false;

                            // Check to make sure sprite index is valid
                            if (index > m_currentSpriteAsset.spriteInfoList.Count - 1) return false;

                            m_spriteIndex = index;
                        }
                        else
                        {
                            return false;
                        }

                        // Material HashCode for the Sprite Asset is the Sprite Asset Hash Code
                        m_currentMaterialIndex = MaterialReference.AddMaterialReference(m_currentSpriteAsset.material, m_currentSpriteAsset, m_materialReferences, m_materialReferenceIndexLookup);

                        //m_materialReferenceStack.Add(m_materialReferenceManager.materialReferences[m_currentMaterialIndex]);


                        m_spriteColor = s_colorWhite;
                        m_tintSprite = false;

                        // Handle Tint Attribute
                        if (m_xmlAttribute[1].nameHashCode == 45819)
                            m_tintSprite = ConvertToFloat(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength, m_xmlAttribute[1].valueDecimalIndex) != 0;
                        else if (m_xmlAttribute[2].nameHashCode == 45819)
                            m_tintSprite = ConvertToFloat(m_htmlTag, m_xmlAttribute[2].valueStartIndex, m_xmlAttribute[2].valueLength, m_xmlAttribute[2].valueDecimalIndex) != 0;

                        // Handle Color Attribute
                        if (m_xmlAttribute[1].nameHashCode == 281955)
                            m_spriteColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[1].valueStartIndex, m_xmlAttribute[1].valueLength);
                        else if (m_xmlAttribute[2].nameHashCode == 281955)
                            m_spriteColor = HexCharsToColor(m_htmlTag, m_xmlAttribute[2].valueStartIndex, m_xmlAttribute[2].valueLength);

                        m_xmlAttribute[1].nameHashCode = 0;
                        m_xmlAttribute[2].nameHashCode = 0;

                        m_textElementType = TMP_TextElementType.Sprite;
                        return true;
                    case 730022849: // <lowercase>
                        m_style |= FontStyles.LowerCase;
                        return true;
                    case -1668324918: // </lowercase>
                        m_style &= ~FontStyles.LowerCase;
                        return true;
                    case 13526026: // <allcaps>
                    case 781906058: // <uppercase>
                        m_style |= FontStyles.UpperCase;
                        return true;
                    case 52232547: // </allcaps>
                    case -1616441709: // </uppercase>
                        m_style &= ~FontStyles.UpperCase;
                        return true;
                    case 766244328: // <smallcaps>
                        m_style |= FontStyles.SmallCaps;
                        return true;
                    case -1632103439: // </smallcaps>
                        m_style &= ~FontStyles.SmallCaps;
                        return true;
                    case 2109854: // <margin=00.0> <margin=00em> <margin=50%>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex); // px
                        if (value == -9999 || value == 0) return false;

                        m_marginLeft = value;
                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                // Default behavior
                                break;
                            case TagUnits.FontUnits:
                                m_marginLeft *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * m_marginLeft / 100;
                                break;
                        }
                        m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                        m_marginRight = m_marginLeft;
                        return true;
                    case 7639357: // </margin>
                        m_marginLeft = 0;
                        m_marginRight = 0;
                        return true;
                    case 1100728678: // <margin-left=xx.x>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex); // px
                        if (value == -9999 || value == 0) return false;

                        m_marginLeft = value;
                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                // Default behavior
                                break;
                            case TagUnits.FontUnits:
                                m_marginLeft *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                m_marginLeft = (m_marginWidth - (m_width != -1 ? m_width : 0)) * m_marginLeft / 100;
                                break;
                        }
                        m_marginLeft = m_marginLeft >= 0 ? m_marginLeft : 0;
                        return true;
                    case -884817987: // <margin-right=xx.x>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex); // px
                        if (value == -9999 || value == 0) return false;

                        m_marginRight = value;
                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                // Default behavior
                                break;
                            case TagUnits.FontUnits:
                                m_marginRight *= m_fontScale * m_fontAsset.fontInfo.TabWidth / m_fontAsset.tabSize;
                                break;
                            case TagUnits.Percentage:
                                m_marginRight = (m_marginWidth - (m_width != -1 ? m_width : 0)) * m_marginRight / 100;
                                break;
                        }
                        m_marginRight = m_marginRight >= 0 ? m_marginRight : 0;
                        return true;
                    case 1109349752: // <line-height=xx.x>
                        value = ConvertToFloat(m_htmlTag, m_xmlAttribute[0].valueStartIndex, m_xmlAttribute[0].valueLength, m_xmlAttribute[0].valueDecimalIndex);
                        if (value == -9999 || value == 0) return false;

                        m_lineHeight = value;
                        switch (tagUnits)
                        {
                            case TagUnits.Pixels:
                                //m_lineHeight *= m_isOrthographic ? 1 : 0.1f;
                                break;
                            case TagUnits.FontUnits:
                                m_lineHeight *= m_fontAsset.fontInfo.LineHeight * m_fontScale;
                                break;
                            case TagUnits.Percentage:
                                m_lineHeight = m_fontAsset.fontInfo.LineHeight * m_lineHeight / 100 * m_fontScale;
                                break;
                        }
                        return true;
                    case -445573839: // </line-height>
                        m_lineHeight = 0;
                        return true;
                    case 15115642: // <noparse>
                        tag_NoParsing = true;
                        return true;
                    case 1913798: // <action>
                        int actionID = m_xmlAttribute[0].valueHashCode;

                        if (m_isParsingText)
                        {
                            m_actionStack.Add(actionID);

                            Debug.Log("Action ID: [" + actionID + "] First character index: " + m_characterCount);

                            
                        }
                        //if (m_isParsingText)
                        //{
                        // TMP_Action action = TMP_Action.GetAction(m_xmlAttribute[0].valueHashCode);
                        //}
                        return true;
                    case 7443301: // </action>
                        if (m_isParsingText)
                        {
                            Debug.Log("Action ID: [" + m_actionStack.CurrentItem() + "] Last character index: " + (m_characterCount - 1));
                        }

                        m_actionStack.Remove();
                        return true;
                }
            }
            return false;
        }
    }
}
