﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Baum2.Editor
{
	public abstract class Element
	{
		public static Dictionary<string, Func<Dictionary<string, object>, Element>> Generator = new Dictionary<string, Func<Dictionary<string, object>, Element>>()
		{
			{ "Root", (d) => { return new RootElement(d); } },
			{ "Image", (d) => { return new ImageElement(d); } },
			{ "Mask", (d) => { return new MaskElement(d); } },
			{ "Group", (d) => { return new GroupElement(d); } },
			{ "Text", (d) => { return new TextElement(d); } },
			{ "Button", (d) => { return new ButtonElement(d); } },
			{ "List", (d) => { return new ListElement(d); } },
			{ "Slider", (d) => { return new SliderElement(d); } },
		};

		public string Name;

		public static Element Generate(Dictionary<string, object> json)
		{
			var type = json.Get("type");
			Assert.IsTrue(Generator.ContainsKey(type), "[Baum2] Unknown type: " + type);
			return Generator[type](json);
		}

		public abstract GameObject Render(Renderer renderer);
		public abstract Area CalcArea();
	}

	public class GroupElement : Element
	{
		protected string pivot;
		protected List<Element> elements;

		public GroupElement(Dictionary<string, object> json)
		{
			Name = json.Get("name");
			if (json.ContainsKey("pivot")) pivot = json.Get("pivot");

			elements = new List<Element>();
			var jsonElements = json.Get<List<object>>("elements");
			foreach (var jsonElement in jsonElements)
			{
				elements.Add(Element.Generate(jsonElement as Dictionary<string, object>));
			}
			elements.Reverse();
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = CreateSelf(renderer);

			RenderChildren(renderer, go);

			return go;
		}

		protected virtual GameObject CreateSelf(Renderer renderer)
		{
			var go = PrefabCreator.CreateUIGameObject(Name);

			var rect = go.GetComponent<RectTransform>();
			var area = CalcArea();
			rect.sizeDelta = area.Size;
			rect.localPosition = renderer.CalcPosition(area.Min, area.Size);

			SetMaskImage(renderer, go);
			return go;
		}

		protected void SetMaskImage(Renderer renderer, GameObject go)
		{
			var maskSource = elements.Find(x => x is MaskElement);
			if (maskSource == null) return;

			elements.Remove(maskSource);
			var maskImage = go.AddComponent<Image>();
			maskImage.raycastTarget = false;

			var dummyMaskImage = maskSource.Render(renderer);
			dummyMaskImage.transform.SetParent(go.transform);
			dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
			GameObject.DestroyImmediate(dummyMaskImage);

			var mask = go.AddComponent<Mask>();
			mask.showMaskGraphic = false;
		}

		protected void RenderChildren(Renderer renderer, GameObject root, Action<GameObject, Element> callback = null)
		{
			foreach (var element in elements)
			{
				var go = element.Render(renderer);
				var size = go.GetComponent<RectTransform>().sizeDelta;
				go.transform.SetParent(root.transform, true);
				go.GetComponent<RectTransform>().sizeDelta = size;
				if (element is GroupElement) ((GroupElement)element).SetPivot(go, renderer);
				if (callback != null) callback(go, element);
			}
		}

		protected void SetPivot(GameObject root, Renderer renderer)
		{
			if (string.IsNullOrEmpty(pivot)) pivot = "none";

			var rect = root.GetComponent<RectTransform>();
			var pivotPos = new Vector2(0.5f, 0.5f);

			var originalPosition = root.GetComponent<RectTransform>().anchoredPosition;
			Vector2 canvasSize = renderer.CanvasSize;

			if (pivot.Contains("Bottom") || pivot.Contains("bottom"))
			{
				pivotPos.y = 0.0f;
				originalPosition.y = originalPosition.y + canvasSize.y / 2.0f;
			}
			else if (pivot.Contains("Top") || pivot.Contains("top"))
			{
				pivotPos.y = 1.0f;
				originalPosition.y = originalPosition.y - canvasSize.y / 2.0f;
			}
			if (pivot.Contains("Left") || pivot.Contains("left"))
			{
				pivotPos.x = 0.0f;
				originalPosition.x = originalPosition.x + canvasSize.x / 2.0f;
			}
			else if (pivot.Contains("Right") || pivot.Contains("right"))
			{
				pivotPos.x = 1.0f;
				originalPosition.x = originalPosition.x - canvasSize.x / 2.0f;
			}

			rect.anchorMin = pivotPos;
			rect.anchorMax = pivotPos;
			rect.anchoredPosition = originalPosition;
		}

		public override Area CalcArea()
		{
			var area = Area.None();
			foreach (var element in elements) area.Merge(element.CalcArea());
			return area;
		}
	}

	public class RootElement : GroupElement
	{
		public RootElement(Dictionary<string, object> json) : base(json)
		{
		}

		protected override GameObject CreateSelf(Renderer renderer)
		{
			var go = PrefabCreator.CreateUIGameObject(Name);

			var rect = go.GetComponent<RectTransform>();
			var area = CalcArea();
			rect.sizeDelta = area.Size;
			rect.localPosition = Vector2.zero;

			SetMaskImage(renderer, go);
			return go;
		}
	}

	public class ImageElement : Element
	{
		private string spriteName;
		private Vector2 canvasPosition;
		private Vector2 sizeDelta;
		private float opacity;
		private bool background;

		public ImageElement(Dictionary<string, object> json)
		{
			Name = json.Get("name");
			spriteName = json.Get("image");
			canvasPosition = json.GetVector2("x", "y");
			sizeDelta = json.GetVector2("w", "h");
			opacity = json.GetFloat("opacity");
			if (json.ContainsKey("background")) background = (bool)json["background"];
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = PrefabCreator.CreateUIGameObject(Name);

			var rect = go.GetComponent<RectTransform>();
			rect.localPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
			rect.sizeDelta = sizeDelta;

			var image = go.AddComponent<Image>();
			image.sprite = renderer.GetSprite(spriteName);
			image.raycastTarget = false;
			image.type = Image.Type.Sliced;
			image.color = new Color(1.0f, 1.0f, 1.0f, opacity / 100.0f);

			if (background)
			{
				image.raycastTarget = true;
				rect.anchorMin = Vector2.zero;
				rect.anchorMax = Vector2.one;
				rect.sizeDelta = Vector2.zero;
			}

			return go;
		}

		public override Area CalcArea()
		{
			return Area.FromPositionAndSize(canvasPosition, sizeDelta);
		}
	}

	public sealed class MaskElement : ImageElement
	{
		public MaskElement(Dictionary<string, object> json) : base(json)
		{
		}
	}

	public sealed class TextElement : Element
	{
		private string message;
		private string font;
		private int fontSize;
		private string align;
		private float virtualHeight;
		private Color fontColor;
		private Vector2 canvasPosition;
		private Vector2 sizeDelta;

		public TextElement(Dictionary<string, object> json)
		{
			Name = json.Get("name");
			message = json.Get("text");
			font = json.Get("font");
			fontSize = json.GetInt("size");
			align = json.Get("align");
			fontColor = EditorUtil.HexToColor(json.Get("color"));
			sizeDelta = json.GetVector2("w", "h");
			canvasPosition = json.GetVector2("x", "y");
			virtualHeight = json.GetFloat("vh");
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = PrefabCreator.CreateUIGameObject(Name);

			var rect = go.GetComponent<RectTransform>();
			rect.localPosition = renderer.CalcPosition(canvasPosition, sizeDelta);
			rect.sizeDelta = sizeDelta;

			var text = go.AddComponent<Text>();
			text.text = message;
			text.font = renderer.GetFont(font);
			text.fontSize = fontSize;
			text.color = fontColor;
			text.horizontalOverflow = HorizontalWrapMode.Overflow;
			text.verticalOverflow = VerticalWrapMode.Overflow;

			var fixedPos = rect.localPosition;
			switch (align)
			{
				case "left":
					text.alignment = TextAnchor.MiddleLeft;
					rect.pivot = new Vector2(0.0f, 0.5f);
					fixedPos.x -= sizeDelta.x / 2.0f;
					break;

				case "center":
					text.alignment = TextAnchor.MiddleCenter;
					rect.pivot = new Vector2(0.5f, 0.5f);
					break;

				case "right":
					text.alignment = TextAnchor.MiddleRight;
					rect.pivot = new Vector2(1.0f, 0.5f);
					fixedPos.x += sizeDelta.x / 2.0f;
					break;
			}
			rect.localPosition = fixedPos;

			var d = rect.sizeDelta;
			d.y = virtualHeight;
			rect.sizeDelta = d;

			return go;
		}

		public override Area CalcArea()
		{
			return Area.FromPositionAndSize(canvasPosition, sizeDelta);
		}
	}

	public sealed class ButtonElement : GroupElement
	{
		public ButtonElement(Dictionary<string, object> json) : base(json)
		{
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = CreateSelf(renderer);

			Graphic lastImage = null;
			RenderChildren(renderer, go, (g, element) =>
			{
				if (lastImage == null && element is ImageElement) lastImage = g.GetComponent<Image>();
			});

			var button = go.AddComponent<Button>();
			if (lastImage != null)
			{
				button.targetGraphic = lastImage;
				lastImage.raycastTarget = true;
			}

			return go;
		}
	}

	public sealed class ListElement : GroupElement
	{
		private string scroll;

		public ListElement(Dictionary<string, object> json) : base(json)
		{
			if (json.ContainsKey("scroll")) scroll = json.Get("scroll");
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = CreateSelf(renderer);
			var content = new GameObject("Content");
			content.AddComponent<RectTransform>();
			content.transform.SetParent(go.transform);

			SetupScroll(go, content);
			SetMaskImage(renderer, go, content);

			var item = CreateItem(renderer, go);
			SetupList(go, item);

			return go;
		}

		private void SetupScroll(GameObject go, GameObject content)
		{
			var scrollRect = go.AddComponent<ScrollRect>();
			scrollRect.content = content.GetComponent<RectTransform>();

			if (scroll == "Vertical")
			{
				var layoutGroup = content.AddComponent<VerticalLayoutGroup>();
				scrollRect.vertical = true;
				scrollRect.horizontal = false;
				layoutGroup.childForceExpandWidth = true;
				layoutGroup.childForceExpandHeight = false;
			}
			else if (scroll == "Horizontal")
			{
				var layoutGroup = content.AddComponent<HorizontalLayoutGroup>();
				scrollRect.vertical = false;
				scrollRect.horizontal = true;
				layoutGroup.childForceExpandWidth = false;
				layoutGroup.childForceExpandHeight = true;
			}
		}

		private void SetMaskImage(Renderer renderer, GameObject go, GameObject content)
		{
			var maskImage = go.AddComponent<Image>();

			var dummyMaskImage = CreateDummyMaskImage(renderer);
			dummyMaskImage.transform.SetParent(go.transform);
			dummyMaskImage.GetComponent<RectTransform>().CopyTo(go.GetComponent<RectTransform>());
			dummyMaskImage.GetComponent<RectTransform>().CopyTo(content.GetComponent<RectTransform>());
			dummyMaskImage.GetComponent<Image>().CopyTo(maskImage);
			GameObject.DestroyImmediate(dummyMaskImage);

			var mask = go.AddComponent<Mask>();
			mask.showMaskGraphic = false;
		}

		private GameObject CreateDummyMaskImage(Renderer renderer)
		{
			var maskElement = elements.Find(x => (x is ImageElement && x.Name == "Area"));
			if (maskElement == null) throw new Exception(string.Format("{0} Area not found", Name));
			elements.Remove(maskElement);

			var maskImage = maskElement.Render(renderer);
			maskImage.SetActive(false);
			return maskImage;
		}

		private GameObject CreateItem(Renderer renderer, GameObject go)
		{
			if (elements.Count != 1) throw new Exception(string.Format("{0} List error", Name));
			var item = elements[0] as GroupElement;
			if (item == null) throw new Exception(string.Format("{0} List error", Name));

			var itemObject = item.Render(renderer);
			var layout = itemObject.AddComponent<LayoutElement>();
			if (scroll == "Vertical") layout.minHeight = item.CalcArea().Height;
			else if (scroll == "Horizontal") layout.minWidth = item.CalcArea().Width;

			itemObject.transform.SetParent(go.transform);
			itemObject.SetActive(false);
			return itemObject;
		}

		private void SetupList(GameObject go, GameObject item)
		{
			var list = go.AddComponent<List>();
			list.ItemSource = item;
		}
	}

	public sealed class SliderElement : GroupElement
	{
		public SliderElement(Dictionary<string, object> json) : base(json)
		{
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = CreateSelf(renderer);

			RectTransform fillRect = null;
			RenderChildren(renderer, go, (g, element) =>
			{
				var image = element as ImageElement;
				if (fillRect != null || image == null) return;
				if (element.Name == "Fill") fillRect = g.GetComponent<RectTransform>();
			});

			var slider = go.AddComponent<Slider>();
			slider.transition = Selectable.Transition.None;
			if (fillRect != null)
			{
				fillRect.localScale = Vector2.zero;
				fillRect.anchorMin = Vector2.zero;
				fillRect.anchorMax = Vector2.one;
				fillRect.anchoredPosition = Vector2.zero;
				fillRect.sizeDelta = Vector2.zero;
				fillRect.localScale = Vector3.one;
				slider.fillRect = fillRect;
			}

			return go;
		}
	}

	public sealed class NullElement : Element
	{
		public NullElement(Dictionary<string, object> json)
		{
			Name = json.Get("name");
		}

		public override GameObject Render(Renderer renderer)
		{
			var go = PrefabCreator.CreateUIGameObject(Name);
			return go;
		}

		public override Area CalcArea()
		{
			return Area.None();
		}
	}
}
