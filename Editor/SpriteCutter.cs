using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// This is only useful for sprite sheets that need to be automatically sliced (Sprite Editor > Slice > Automatic)
// and can also use size to cut sprite
namespace Editor
{
	public abstract class AutoSpriteSlicer
	{
		private const int SpriteSizeX = 54;
		private const int SpriteSizeY = 54;
		private static string spriteCutMode = "size"; // mode can be "size" or "auto"

		[MenuItem("Tools/Slice Sprite sheets auto %&a")]
		public static void SliceAuto()
		{
			var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
			spriteCutMode = "auto";

			foreach (var texture in textures)
			{
				ProcessTexture(texture);
			}
		}
		
		[MenuItem("Tools/Slice Sprite sheets size %&s")]
		public static void SliceSize()
		{
			var textures = Selection.GetFiltered<Texture2D>(SelectionMode.Assets);
			spriteCutMode = "size";

			foreach (var texture in textures)
			{
				ProcessTexture(texture);
			}
		}

		private static void ProcessTexture(Texture2D texture)
		{
			var path = AssetDatabase.GetAssetPath(texture);
			var importer = AssetImporter.GetAtPath(path) as TextureImporter;

			//importer.isReadable = true;
			if (importer != null)
			{
				importer.textureType = TextureImporterType.Sprite;
				importer.spriteImportMode = SpriteImportMode.Multiple;
				importer.mipmapEnabled = false;
				importer.filterMode = FilterMode.Point;
				importer.spritePivot = Vector2.down;
				importer.textureCompression = TextureImporterCompression.Uncompressed;

				var textureSettings =
					new TextureImporterSettings(); // need this stupid class because spriteExtrude and spriteMeshType aren't exposed on TextureImporter
				importer.ReadTextureSettings(textureSettings);
				textureSettings.spriteMeshType = SpriteMeshType.Tight;
				textureSettings.spriteExtrude = 0;

				importer.SetTextureSettings(textureSettings);
				Rect[] rects = new Rect[] { };
				switch (spriteCutMode)
				{
					case "auto":
					{
						const int minimumSpriteSize = 16;
						const int extrudeSize = 0;
						rects = InternalSpriteUtility.GenerateAutomaticSpriteRectangles(texture, minimumSpriteSize, extrudeSize);
						break;
					}
					case "size":
					{
						var spriteSize = new Vector2(SpriteSizeX, SpriteSizeY);
						rects =
							InternalSpriteUtility.GenerateGridSpriteRectangles(texture, Vector2.zero, spriteSize, Vector2.zero);
						break;
					}
				}
				var rectsList = new List<Rect>(rects);
				rectsList = SortRects(rectsList, texture.width);

				var filenameNoExtension = Path.GetFileNameWithoutExtension(path);
				var rectNum = 0;

				importer.spritesheet = rectsList.Select(rect => new SpriteMetaData { pivot = Vector2.down, alignment = (int)SpriteAlignment.BottomCenter, rect = rect, name = filenameNoExtension + "_" + rectNum++ }).ToArray();
			}

			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		}

		private static List<Rect> SortRects(List<Rect> rects, float textureWidth)
		{
			var list = new List<Rect>();
			while (rects.Count > 0)
			{
				Rect rect = rects[^1];
				Rect sweepRect = new Rect(0f, rect.yMin, textureWidth, rect.height);
				List<Rect> list2 = RectSweep(rects, sweepRect);
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
			List<Rect> result;
			if (rects == null || rects.Count == 0)
			{
				result = new List<Rect>();
			}
			else
			{
				var list = rects.Where(current => current.Overlaps(sweepRect)).ToList();
				foreach (var current2 in list)
				{
					rects.Remove(current2);
				}
				list.Sort((a, b) => a.x.CompareTo(b.x));
				result = list;
			}
			return result;
		}
	}
}