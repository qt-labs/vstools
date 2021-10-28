$include$
#include "$pluginheaderfilename$"

#include <QtCore/QtPlugin>

$plugin_class$::$plugin_class$(QObject *parent)
    : QObject(parent)
{
    initialized = false;
}

void $plugin_class$::initialize(QDesignerFormEditorInterface * /*core*/)
{
    if (initialized)
        return;

    initialized = true;
}

bool $plugin_class$::isInitialized() const
{
    return initialized;
}

QWidget *$plugin_class$::createWidget(QWidget *parent)
{
    return new $classname$(parent);
}

QString $plugin_class$::name() const
{
    return "$classname$";
}

QString $plugin_class$::group() const
{
    return "My Plugins";
}

QIcon $plugin_class$::icon() const
{
    return QIcon();
}

QString $plugin_class$::toolTip() const
{
    return QString();
}

QString $plugin_class$::whatsThis() const
{
    return QString();
}

bool $plugin_class$::isContainer() const
{
    return false;
}

QString $plugin_class$::domXml() const
{
    return "<widget class=\"$classname$\" name=\"$objname$\">\n"
        " <property name=\"geometry\">\n"
        "  <rect>\n"
        "   <x>0</x>\n"
        "   <y>0</y>\n"
        "   <width>100</width>\n"
        "   <height>100</height>\n"
        "  </rect>\n"
        " </property>\n"
        "</widget>\n";
}

QString $plugin_class$::includeFile() const
{
    return "$headerfilename$";
}
