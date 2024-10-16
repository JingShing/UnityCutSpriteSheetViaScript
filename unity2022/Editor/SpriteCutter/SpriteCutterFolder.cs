using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEditorInternal;
using UnityEngine;

namespace Plugins.Jingle.Editor.SpriteCut
{
    public static class AutoSpriteSlicerFolder
    {
        private const int SpriteSizeX = 32;
        private const int SpriteSizeY = 32;

        [MenuItem("Tools/Slice Images in Folder")]
        public static void SliceImagesInFolder()
        {
            var selectedFolderPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                Debug.LogError("No folder selected.");
                return;
            }

            // Convert absolute path to relative path
            selectedFolderPath = "Assets" + selectedFolderPath.Substring(Application.dataPath.Length);

            var imagePaths = Directory.GetFiles(selectedFolderPath, "*.png", SearchOption.TopDirectoryOnly);
            foreach (var imagePath in imagePaths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                if (texture != null)
                {
                    ProcessTexture(texture);
                }
            }
        }

        private static void ProcessTexture(Texture2D texture)
        {
            var path = AssetDatabase.GetAssetPath(texture);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Point;
                importer.spritePivot = Vector2.down;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                var textureSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.Tight;
                textureSettings.spriteExtrude = 0;

                importer.SetTextureSettings(textureSettings);
                var spriteData = new SpriteDataProviderFactories();

                // Use SpriteDataProviderFactories to get the ISpriteEditorDataProvider
                if (spriteData.GetSpriteEditorDataProviderFromObject(importer) is { } provider)
                {
                    provider.InitSpriteEditorDataProvider();

                    var spriteSize = new Vector2(SpriteSizeX, SpriteSizeY);
                    var rects = InternalSpriteUtility.GenerateGridSpriteRectangles(texture, Vector2.zero, spriteSize, Vector2.zero);
                    var rectsList = new List<Rect>(rects);
                    rectsList = SortRects(rectsList, texture.width);

                    var filenameNoExtension = Path.GetFileNameWithoutExtension(path);
                    var rectNum = 0;

                    // Use SpriteRect[] instead of SpriteMetaData[]
                    var spriteRects = new SpriteRect[rectsList.Count];
                    for (var i = 0; i < rectsList.Count; i++)
                    {
                        spriteRects[i] = new SpriteRect
                        {
                            name = $"{filenameNoExtension}_{rectNum++}",
                            rect = rectsList[i],
                            alignment = SpriteAlignment.BottomCenter,
                            pivot = Vector2.down
                        };
                    }

                    provider.SetSpriteRects(spriteRects);
                    provider.Apply();
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        private static List<Rect> SortRects(List<Rect> rects, float textureWidth)
        {
            var list = new List<Rect>();
            while (rects.Count > 0)
            {
                var rect = rects[^1];
                var sweepRect = new Rect(0f, rect.yMin, textureWidth, rect.height);
                var list2 = RectSweep(rects, sweepRect);
                if (list2.Count <= 0)
                {
                    list.AddRange(rects);
                    break;
                }
                list.AddRange(list2);
            }
            return list;
        }

        static List<Rect> RectSweep(List<Rect> rects, Rect sweepRect)
        {
            if (rects == null || rects.Count == 0)
            {
                return new List<Rect>();
            }

            var list = rects.Where(current => current.Overlaps(sweepRect)).ToList();
            foreach (var current2 in list)
            {
                rects.Remove(current2);
            }
            list.Sort((a, b) => a.x.CompareTo(b.x));
            return list;
        }
    }
}
