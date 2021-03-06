baum2
=====

## Setup

### Photoshop

* Download [Baum.js](https://raw.githubusercontent.com/kyubuns/Baum2/master/PhotoshopScript/Baum.js) & [json2.min.js](https://raw.githubusercontent.com/kyubuns/Baum2/master/PhotoshopScript/lib/json2.min.js)
* Copy to Photoshop/Plugins directory Baum.js & **lib/** json2.min.js
    - Mac OS: Applications\Adobe Photoshop [Photoshop_version]\Presets\Scripts
    - Windows 32 bit: Program Files (x86)\Adobe\Adobe Photoshop [Photoshop_version]\Presets\Scripts
    - Windows 64 bit: Program Files\Adobe\Adobe Photoshop [Photoshop_version](64 Bit)\Presets\Scripts

![lib 2018-03-19 13-16-04](https://user-images.githubusercontent.com/961165/37577816-ca588980-2b77-11e8-991b-91346d33c507.png)

### Unity

* Download & Import [baum2.unitypackage](https://github.com/kyubuns/Baum2/blob/master/Baum2.unitypackage?raw=true)
* Baum2/Sample以下を参考に、好きなディレクトリに「BaumSprites」「BaumPrefabs」「BaumFonts」という空のファイルを作ってください。
    * BaumSprites: psdから生成されたSpriteを保存するディレクトリになります。
    * BaumPrefabs: psdから生成されたPrefabを保存するディレクトリになります。
    * BaumFonts: Prefabを作る際に使用するFontの参照先になります。
* psd上で使用するFontは、BaumFontsファイルを置いたディレクトリに置いておいてください。

## 使い方

### Photoshop上での操作

* psdを作ります。(psdの作り方参照)
* File -> Scripts -> Baum2を選択し、中間ファイルの出力先を選択します。

### Unity上での操作

* 生成された中間ファイルをBaum2/Importディレクトリ以下に投げ込みます。
* 自動的に「BaumPrefabs」を配置したディレクトリにprefabが出来上がります。
* 後は、Sample/Sample.csを参考にスクリプトからBaumUI.Instantiateで実行時に生成してください。

### psdの更新方法

* 同じように中間ファイルを生成後、Baum2/Importディレクトリ以下に投げ込むと、prefabが上書き更新されます。
    * この時、prefabのGUIDは変更されないためScriptに対する参照を張り直す必要はありません。

## psdの作り方

### 基本

基本的にPhotoshop上の1レイヤー = Unity上の1GameObjectになります。  
UIの一部をアニメーションさせたい場合などは、Photoshop上のレイヤーを分けておいてください。  

### Text

* Photoshop上の **Textレイヤー** は、Unity上でUnityEngine.UI.Textとして変換されます。
* フォントやフォントサイズ、色などの情報も可能な限りUnity側も同じように設定されます。

### Button

* Photoshop上の **名前が"Button"で終わるグループ** は、Unity上でUnityEngine.UI.Buttonとして変換されます。
* このグループ内で、最も奥に描画されるイメージレイヤーがクリック可能な範囲(UI.Button.TargetGraphic)に設定されます。

### Slider

* Photoshop上の **名前が"Slider"で終わるグループ** は、Unity上でUnityEngine.UI.Sliderとして変換されます。
* このグループ内で、名前がFillになっているイメージレイヤーがスライドするイメージ(UI.Slider.FillRect)になります。

### Scrollbar

* Photoshop上の **名前が"Scrollbar"で終わるグループ** は、Unity上でUnityEngine.UI.Scrollbarとして変換されます。
* このグループ内で、名前がHandleになっているイメージレイヤーがスライドするハンドル(UI.Scrollbar.HandleRect)になります。

### List

* Photoshop上の **名前が"List"で終わるグループ** は、Unity上でBaum2.Listとして変換されます。
* このグループ内には、Itemグループと、Maskレイヤーが必須です。
    * Itemグループ内の要素がリストの1アイテムになります。
    * Maskレイヤーがそのリストにかかるマスクになります。
* 詳しくはサンプルをご覧ください。

### Pivot

* Photoshop上のルート直下にあるグループにのみ使えます。
* 名前の後ろに *@Pivot=TopRight* のようにPivotを指定できます。

### コメントレイヤー

レイヤー名の先頭に#をつけることで、出力されないレイヤーを作ることが出来ます。

## Developed by

* Unity: Unity2017
* PhotoshopScript: Adobe Photoshop CC 2018
