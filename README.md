# Asyntai AI Chatbot for nopCommerce

Adds the [Asyntai](https://asyntai.com/) AI chat assistant to every page of a nopCommerce store. The assistant answers shoppers day and night, in 36 languages, from the store's own products, pages and documents.

## Requirements

- nopCommerce 4.90 or 5.00
- An Asyntai account (free plan available)

## Install

1. In the admin area, open **Configuration > Local plugins** and press **Upload plugin or theme**.
2. Upload `asyntai-nopcommerce-1.10.zip` from the [latest release](https://github.com/asyntai/nopcommerce-chatbot/releases/latest). The same zip works on 4.90 and 5.00.
3. Install **Asyntai AI Chatbot**, then press **Restart application to apply changes**.
4. Press **Configure**, keep **Let the chatbot read your products and pages** ticked and press **Get started**.
5. Sign in to Asyntai or create a free account in the window that opens.

## Settings

- **Let the chatbot read your products and pages**: the chatbot answers from your published products (with current prices), categories and topic pages. Switch it off at any time.
- **Widget ID**: your Asyntai widget ID. **Get started** fills it in; you can also paste it from **Setup & Integration** in the dashboard.
- **Pass customer details**: greet a signed-in customer by name.
- **Hide on checkout**: keep the chat off the checkout pages.
- **Script URL**: the widget script address. Keep the default.

Widget ID, customer details, checkout and script settings can be set per store.

## How products and pages are shared

With sharing on, the store answers `/asyntai-feed/products` and `/asyntai-feed/pages`. Both answer only requests signed with a secret that is made when the store connects, and refuse everything else. They hold only what a guest of the store can see. The secret is never printed into a page, and **Disconnect** deletes it.

## Build from source

1. Get the nopCommerce source (5.00, or 4.90).
2. Copy `Nop.Plugin.Widgets.Asyntai` into `src/Plugins/`.
3. For 4.90 only: set `TargetFramework` to `net9.0` in the `.csproj` and `SupportedVersions` to `[ "4.90" ]` in `plugin.json`.
4. Run `dotnet build src/Plugins/Nop.Plugin.Widgets.Asyntai/Nop.Plugin.Widgets.Asyntai.csproj -c Release -p:SolutionDir=<path>/src/`.
   The output lands in `src/Presentation/Nop.Web/Plugins/Widgets.Asyntai`.

## Uninstall

Uninstall the plugin in **Configuration > Local plugins**. The chat bubble is removed and the plugin settings are deleted.

## Support

- Documentation: https://asyntai.com/documentation/integrations/nopcommerce/
- Email: hello@asyntai.com
