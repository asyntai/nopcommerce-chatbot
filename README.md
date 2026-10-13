# Asyntai AI Chatbot for nopCommerce

Adds the [Asyntai](https://asyntai.com/) AI chat assistant to every page of a nopCommerce store. The assistant answers shoppers day and night, in 36 languages, from the store's own products, pages and documents.

## Requirements

- nopCommerce 4.90
- An Asyntai account (free plan available)

## Install

1. In the admin area, open **Configuration > Local plugins** and press **Upload plugin or theme**.
2. Upload `asyntai-nopcommerce-1.00.zip` from the [latest release](https://github.com/asyntai/nopcommerce-chatbot/releases/latest).
3. Install **Asyntai AI Chatbot**, then press **Restart application to apply changes**.
4. Press **Configure**, paste the widget ID from your Asyntai dashboard (**Setup & Integration**) and press **Save**.

## Settings

- **Widget ID**: your Asyntai widget ID.
- **Pass customer details**: greet a signed-in customer by name.
- **Hide on checkout**: keep the chat off the checkout pages.
- **Script URL**: the widget script address. Keep the default.

Every setting can be set per store.

## Build from source

1. Get the nopCommerce 4.90 source.
2. Copy `Nop.Plugin.Widgets.Asyntai` into `src/Plugins/`.
3. Run `dotnet build src/Plugins/Nop.Plugin.Widgets.Asyntai/Nop.Plugin.Widgets.Asyntai.csproj -c Release -p:SolutionDir=<path>/src/`.
   The output lands in `src/Presentation/Nop.Web/Plugins/Widgets.Asyntai`.

## Uninstall

Uninstall the plugin in **Configuration > Local plugins**. The chat bubble is removed and the plugin settings are deleted.

## Support

- Documentation: https://asyntai.com/documentation/integrations/nopcommerce/
- Email: hello@asyntai.com
