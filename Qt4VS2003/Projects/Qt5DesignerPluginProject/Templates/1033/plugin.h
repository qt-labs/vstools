#ifndef %PLUGIN_HEADER_PRE_DEF%
#define %PLUGIN_HEADER_PRE_DEF%

#include <QtDesigner/QDesignerCustomWidgetInterface>

class %PLUGIN_CLASS% : public QObject, public QDesignerCustomWidgetInterface
{
    Q_OBJECT
    Q_PLUGIN_METADATA(IID "org.qt-project.Qt.QDesignerCustomWidgetInterface" FILE "%PLUGIN_JSON%.json")
    Q_INTERFACES(QDesignerCustomWidgetInterface)

public:
    %PLUGIN_CLASS%(QObject *parent = 0);

    bool isContainer() const;
    bool isInitialized() const;
    QIcon icon() const;
    QString domXml() const;
    QString group() const;
    QString includeFile() const;
    QString name() const;
    QString toolTip() const;
    QString whatsThis() const;
    QWidget *createWidget(QWidget *parent);
    void initialize(QDesignerFormEditorInterface *core);

private:
    bool initialized;
};

#endif // %PLUGIN_HEADER_PRE_DEF%