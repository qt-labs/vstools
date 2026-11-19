#include <QtTest>

class $classname$ : public QObject
{
    Q_OBJECT

private slots:

    void initTestCase_data()
    {
        qDebug("Creates a global test data table.");
    }

    void initTestCase()
    {
        qDebug("Called before the first test function is executed.");
    }

    void init()
    {
        qDebug("Called before each test function is executed.");
    }

    void myTest()
    {
        QVERIFY(true); // check that a condition is satisfied
        QCOMPARE(1, 1); // compare two values
    }

    void cleanup()
    {
        qDebug("Called after every test function.");
    }

    void cleanupTestCase()
    {
        qDebug("Called after the last test function was executed.");
    }
};

QTEST_MAIN($classname$)
#include "$mocfile$"
