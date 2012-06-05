#include "%INCLUDE%"

#include <QtCore/QtPlugin>
#include "%PLUGIN_INCLUDE%"


%PLUGIN_CLASS%::%PLUGIN_CLASS%(QObject *parent)
    : QObject(parent)
{
    initialized = false;
}

void %PLUGIN_CLASS%::initialize(QDesignerFormEditorInterface * /*core*/)
{
    if (initialized)
        return;

    initialized = true;
}

bool %PLUGIN_CLASS%::isInitialized() const
{
    return initialized;
}

QWidget *%PLUGIN_CLASS%::createWidget(QWidget *parent)
{
    return new %CLASS%(parent);
}

QString %PLUGIN_CLASS%::name() const
{
    return "%CLASS%";
}

QString %PLUGIN_CLASS%::group() const
{
    return "My Plugins";
}

QIcon %PLUGIN_CLASS%::icon() const
{
    return QIcon();
}

QString %PLUGIN_CLASS%::toolTip() const
{
    return QString();
}

QString %PLUGIN_CLASS%::whatsThis() const
{
    return QString();
}

bool %PLUGIN_CLASS%::isContainer() const
{
    return false;
}

QString %PLUGIN_CLASS%::domXml() const
{
    return "<widget class=\"%CLASS%\" name=\"%OBJNAME%\">\n"
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

QString %PLUGIN_CLASS%::includeFile() const
{
    return "%HEADERFILE%";
}

